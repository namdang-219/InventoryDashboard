using IID.Domain.Common;

namespace IID.Application.Common.Interfaces;

/// <summary>
/// Handler contract for in-process domain events (pure Clean Architecture / DDD).
/// </summary>
public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task Handle(TEvent domainEvent, CancellationToken cancellationToken);
}
