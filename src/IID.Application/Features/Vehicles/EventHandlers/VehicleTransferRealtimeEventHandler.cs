using IID.Application.Common.Interfaces;
using IID.Domain.Vehicles.Events;
using MediatR;

namespace IID.Application.Features.Vehicles.EventHandlers;

/// <summary>
/// Forwards the <see cref="VehicleTransferred"/> domain event to the SignalR
/// inventory hub, scoped to BOTH the source and target dealership groups.
/// Kept separate from <see cref="VehicleRealtimeEventHandler"/> because the
/// fan-out is two-group-only (not the vehicle's single dealer group).
/// </summary>
public sealed class VehicleTransferRealtimeEventHandler(IVehicleHubNotifier notifier) :
    IDomainEventHandler<VehicleTransferred>
{
    public Task Handle(VehicleTransferred notification, CancellationToken cancellationToken)
        => notifier.VehicleTransferredAsync(
            notification.Vehicle,
            notification.PreviousDealershipId,
            cancellationToken);
}
