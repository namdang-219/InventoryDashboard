using IID.Application.Common.Interfaces;
using IID.Domain.Dealerships.Events;
using MediatR;

namespace IID.Application.Features.Dealerships.EventHandlers;

/// <summary>
/// Forwards <see cref="DealershipAdded"/> and <see cref="DealershipUpdated"/>
/// domain events to the SignalR dashboard group via
/// <see cref="IDealershipHubNotifier"/>. Mirrors the per-vehicle broadcaster
/// pattern (<see cref="Features.Vehicles.EventHandlers.VehicleRealtimeEventHandler"/>).
/// </summary>
public sealed class DealershipRealtimeEventHandler(IDealershipHubNotifier notifier) :
    IDomainEventHandler<DealershipAdded>,
    IDomainEventHandler<DealershipUpdated>
{
    public Task Handle(DealershipAdded notification, CancellationToken cancellationToken)
        => notifier.DealershipAddedAsync(notification.Dealership, cancellationToken);

    public Task Handle(DealershipUpdated notification, CancellationToken cancellationToken)
        => notifier.DealershipUpdatedAsync(notification.Dealership, cancellationToken);
}
