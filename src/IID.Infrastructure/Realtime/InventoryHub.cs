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
    Task VehicleAdded(VehicleResponse vehicle);
    Task VehicleUpdated(VehicleResponse vehicle);
    Task VehicleRemoved(Guid vehicleId);
    Task VehicleAging(VehicleResponse vehicle);
    Task VehicleActionLogged(VehicleActionResponse action);
    Task DashboardSummaryUpdated(DashboardSummaryResponse summary);
    Task DashboardAlertsUpdated(IReadOnlyList<DashboardAlertResponse> alerts);
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

public sealed record VehicleResponse(
    Guid Id, string Vin, string Make, string Model, int Year,
    int DaysInInventory, bool IsAging, string Status, Guid DealershipId = default);

public sealed record VehicleActionResponse(Guid Id, Guid VehicleId, string ActionType, string? Notes, DateTimeOffset LoggedAtUtc, string? VehicleName = null);

public sealed record DashboardSummaryResponse(
    DateTimeOffset GeneratedAtUtc,
    int TotalInventory,
    int AvailableCount,
    int PendingCount,
    int SoldCount,
    int WholesaleCount,
    int AgingCount,
    decimal TotalInventoryValue);

public sealed record DashboardAlertResponse(
    Guid? VehicleId,
    string Message,
    string Severity,
    DateTimeOffset CreatedAtUtc);
