using IID.Application.Common.Interfaces;
using IID.Domain.Vehicles.Events;
using MediatR;

namespace IID.Application.Features.Vehicles.EventHandlers;

/// <summary>
/// Forwards per-vehicle lifecycle domain events to the SignalR inventory hub.
/// Routing is scoped to the vehicle's dealership group by the notifier itself.
///
/// Order is intentional:
/// <list type="bullet">
///   <item><c>VehicleAdded</c>     → <c>notifier.VehicleAddedAsync</c>     (new arrival in a dealer group).</item>
///   <item><c>VehicleUpdated</c>   → <c>notifier.VehicleUpdatedAsync</c>   (edits, status change without sale).</item>
///   <item><c>VehicleSold</c>      → <c>notifier.VehicleUpdatedAsync</c>   (status flips to Sold, all groups reload the row).</item>
///   <item><c>VehicleRemoved</c>   → <c>notifier.VehicleRemovedAsync</c>   (soft delete; broadcast to all groups).</item>
/// </list>
/// <see cref="VehicleStatusChanged"/> is intentionally not handled here — the
/// status change is already implied by VehicleUpdated/VehicleSold and handled
/// by <see cref="DashboardRealtimeEventHandler"/>.
/// </summary>
public sealed class VehicleRealtimeEventHandler(IVehicleHubNotifier notifier) :
    IDomainEventHandler<VehicleAdded>,
    IDomainEventHandler<VehicleUpdated>,
    IDomainEventHandler<VehicleSold>,
    IDomainEventHandler<VehicleRemoved>
{
    public Task Handle(VehicleAdded notification, CancellationToken cancellationToken)
        => notifier.VehicleAddedAsync(notification.Vehicle, cancellationToken);

    public Task Handle(VehicleUpdated notification, CancellationToken cancellationToken)
        => notifier.VehicleUpdatedAsync(notification.Vehicle, cancellationToken);

    public Task Handle(VehicleSold notification, CancellationToken cancellationToken)
        => notifier.VehicleUpdatedAsync(notification.Vehicle, cancellationToken);

    public Task Handle(VehicleRemoved notification, CancellationToken cancellationToken)
        => notifier.VehicleRemovedAsync(notification.Vehicle.Id, cancellationToken);
}
