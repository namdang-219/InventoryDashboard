using IID.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace IID.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Publishes domain events after a successful SaveChanges to IDomainEventHandler implementations.
/// Resolves handlers lazily to avoid a DI cycle with DbContext.
/// </summary>
public sealed class DomainEventDispatchInterceptor(IServiceProvider serviceProvider) : SaveChangesInterceptor
{
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
            await DispatchEventsAsync(eventData.Context, cancellationToken);
        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    private async Task DispatchEventsAsync(DbContext context, CancellationToken cancellationToken)
    {
        var aggregates = context.ChangeTracker
            .Entries<AggregateRoot>()
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Count > 0)
            .ToList();

        var domainEvents = aggregates.SelectMany(a => a.DomainEvents).ToList();
        aggregates.ForEach(a => a.ClearDomainEvents());

        if (domainEvents.Count == 0) return;

        foreach (var domainEvent in domainEvents)
        {
            var eventType = domainEvent.GetType();
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);
            var handlers = serviceProvider.GetServices(handlerType);
            foreach (var handler in handlers)
            {
                if (handler is not null)
                {
                    var method = handlerType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.Handle));
                    if (method is not null)
                    {
                        var task = (Task)method.Invoke(handler, [domainEvent, cancellationToken])!;
                        await task;
                    }
                }
            }
        }
    }
}
