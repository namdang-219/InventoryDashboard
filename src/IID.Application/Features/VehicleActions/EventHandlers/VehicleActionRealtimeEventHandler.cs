using IID.Application.Common.Interfaces;
using IID.Domain.VehicleActions.Events;
using MediatR;

namespace IID.Application.Features.VehicleActions.EventHandlers;

/// <summary>
/// Forwards <see cref="VehicleActionLogged"/> domain events to the SignalR hub
/// and triggers a global inventory-refresh signal so list views refresh their
/// unread/cached counts. The originating command handler is expected to have
/// enriched the event with the originating <see cref="Domain.Vehicles.Vehicle"/>
/// via <c>VehicleAction.EnrichLastActionEventWith</c>.
/// </summary>
public sealed class VehicleActionRealtimeEventHandler(IVehicleHubNotifier notifier) :
    IDomainEventHandler<VehicleActionLogged>
{
    public async Task Handle(VehicleActionLogged notification, CancellationToken cancellationToken)
    {
        if (notification.Vehicle is not null)
        {
            await notifier.VehicleActionLoggedAsync(
                notification.Action,
                notification.Vehicle,
                cancellationToken);
        }
        else
        {
            await notifier.VehicleActionLoggedAsync(notification.Action, cancellationToken);
        }

        await notifier.InventoryChangedAsync(cancellationToken);
    }
}
