using IID.Infrastructure.Realtime.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace IID.Infrastructure.Realtime;

public static class InventoryHubConstants
{
    public const string DashboardGroup = "inventory-dashboard";
    public static string DealershipGroup(Guid dealershipId) => $"dealership:{dealershipId}";
}

public interface IInventoryClient
{
    Task VehicleAdded(VehicleRealtimeDto vehicle);
    Task VehicleUpdated(VehicleRealtimeDto vehicle);
    Task VehicleRemoved(Guid vehicleId);
    Task VehicleAging(VehicleRealtimeDto vehicle);
    Task VehicleActionLogged(VehicleActionRealtimeDto action);
    Task DashboardSummaryUpdated(DashboardSummaryRealtimeDto summary);
    Task DashboardAlertsUpdated(IReadOnlyList<DashboardAlertRealtimeDto> alerts);
    Task InventoryChanged();
}

[Authorize]
public sealed class InventoryHub : Hub<IInventoryClient>
{
    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, InventoryHubConstants.DashboardGroup);
        await base.OnConnectedAsync();
    }

    public async Task JoinDealership(Guid dealershipId)
    {
        if (dealershipId != Guid.Empty)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, InventoryHubConstants.DealershipGroup(dealershipId));
        }
    }

    public async Task LeaveDealership(Guid dealershipId)
    {
        if (dealershipId != Guid.Empty)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, InventoryHubConstants.DealershipGroup(dealershipId));
        }
    }
}
