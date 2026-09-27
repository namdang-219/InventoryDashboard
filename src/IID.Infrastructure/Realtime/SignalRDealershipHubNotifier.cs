using IID.Application.Common.Interfaces;
using IID.Infrastructure.Realtime.Contracts;
using Microsoft.AspNetCore.SignalR;

namespace IID.Infrastructure.Realtime;

/// <summary>
/// SignalR broadcaster for dealership lifecycle events. Routed to the
/// dashboard group (<c>inventory-dashboard</c>) which all connected
/// clients join in <see cref="InventoryHub.OnConnectedAsync"/> — so a
/// single group call covers every dashboard subscriber.
/// </summary>
public sealed class SignalRDealershipHubNotifier(
    IHubContext<InventoryHub, IInventoryClient> hub,
    IRealtimeContractMapper mapper) : IDealershipHubNotifier
{
    public Task DealershipAddedAsync(Domain.Dealerships.Dealership dealership, CancellationToken ct)
        => hub.Clients.Group(InventoryHubConstants.DashboardGroup)
            .DealershipAdded(mapper.MapDealership(dealership));

    public Task DealershipUpdatedAsync(Domain.Dealerships.Dealership dealership, CancellationToken ct)
        => hub.Clients.Group(InventoryHubConstants.DashboardGroup)
            .DealershipUpdated(mapper.MapDealership(dealership));
}
