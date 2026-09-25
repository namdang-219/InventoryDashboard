using IID.Application.Common.Interfaces;
using IID.Application.Dashboard.Dtos;
using Microsoft.AspNetCore.SignalR;

namespace IID.Infrastructure.Realtime;

public sealed class SignalRVehicleHubNotifier(IHubContext<InventoryHub, IInventoryClient> hub) : IVehicleHubNotifier
{
    public Task VehicleAddedAsync(Vehicle v, CancellationToken ct)
    {
        var resp = MapVehicle(v);
        if (v.DealershipId != Guid.Empty)
            return Task.WhenAll(
                hub.Clients.Group(InventoryPolicy.AgingDashboardGroup).VehicleAdded(resp),
                hub.Clients.Group(InventoryPolicy.GetDealershipGroup(v.DealershipId)).VehicleAdded(resp));
        return hub.Clients.Group(InventoryPolicy.AgingDashboardGroup).VehicleAdded(resp);
    }

    public Task VehicleUpdatedAsync(Vehicle v, CancellationToken ct)
    {
        var resp = MapVehicle(v);
        if (v.DealershipId != Guid.Empty)
            return Task.WhenAll(
                hub.Clients.Group(InventoryPolicy.AgingDashboardGroup).VehicleUpdated(resp),
                hub.Clients.Group(InventoryPolicy.GetDealershipGroup(v.DealershipId)).VehicleUpdated(resp));
        return hub.Clients.Group(InventoryPolicy.AgingDashboardGroup).VehicleUpdated(resp);
    }

    public Task VehicleRemovedAsync(Guid vehicleId, CancellationToken ct)
        => hub.Clients.Group(InventoryPolicy.AgingDashboardGroup).VehicleRemoved(vehicleId);

    public Task VehicleAgingAsync(Vehicle v, CancellationToken ct)
    {
        var resp = MapVehicle(v);
        if (v.DealershipId != Guid.Empty)
            return Task.WhenAll(
                hub.Clients.Group(InventoryPolicy.AgingDashboardGroup).VehicleAging(resp),
                hub.Clients.Group(InventoryPolicy.GetDealershipGroup(v.DealershipId)).VehicleAging(resp));
        return hub.Clients.Group(InventoryPolicy.AgingDashboardGroup).VehicleAging(resp);
    }

    public Task VehicleActionLoggedAsync(VehicleAction a, Vehicle? v, CancellationToken ct)
    {
        var vehicleName = v is not null ? $"{v.Year} {v.Make} {v.Model}" : null;
        var resp = new VehicleActionResponse(a.Id, a.VehicleId, a.ActionType.ToString(), a.Notes, a.LoggedAt, vehicleName);
        if (v is not null && v.DealershipId != Guid.Empty)
            return Task.WhenAll(
                hub.Clients.Group(InventoryPolicy.AgingDashboardGroup).VehicleActionLogged(resp),
                hub.Clients.Group(InventoryPolicy.GetDealershipGroup(v.DealershipId)).VehicleActionLogged(resp));
        return hub.Clients.Group(InventoryPolicy.AgingDashboardGroup).VehicleActionLogged(resp);
    }

    public Task VehicleActionLoggedAsync(VehicleAction a, CancellationToken ct)
        => VehicleActionLoggedAsync(a, null, ct);

    public Task DashboardSummaryUpdatedAsync(DashboardSummaryDto summary, CancellationToken ct)
        => hub.Clients.Group(InventoryPolicy.AgingDashboardGroup)
            .DashboardSummaryUpdated(MapSummary(summary));

    public Task DashboardAlertsUpdatedAsync(IReadOnlyList<DashboardAlertDto> alerts, CancellationToken ct)
        => hub.Clients.Group(InventoryPolicy.AgingDashboardGroup)
            .DashboardAlertsUpdated(alerts.Select(MapAlert).ToList());

    public Task InventoryChangedAsync(CancellationToken ct)
        => hub.Clients.Group(InventoryPolicy.AgingDashboardGroup).InventoryChanged();

    private static VehicleResponse MapVehicle(Vehicle v)
    {
        var aging = new AgingStockIdentifier(DateTimeOffset.UtcNow);
        return new VehicleResponse(
            v.Id, v.Vin.Value, v.Make, v.Model, v.Year,
            aging.DaysInInventory(v), aging.IsAging(v), v.Status.ToString(), v.DealershipId);
    }

    private static DashboardSummaryResponse MapSummary(DashboardSummaryDto dto)
        => new(dto.GeneratedAtUtc, dto.TotalInventory, dto.AvailableCount,
            dto.PendingCount, dto.SoldCount, dto.WholesaleCount, dto.AgingCount, dto.TotalInventoryValue);

    private static DashboardAlertResponse MapAlert(DashboardAlertDto dto)
        => new(dto.VehicleId, dto.Message, dto.Severity, dto.CreatedAtUtc);
}
