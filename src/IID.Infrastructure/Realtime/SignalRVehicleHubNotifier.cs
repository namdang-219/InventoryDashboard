using IID.Application.Common.Interfaces;
using IID.Application.Dashboard.Dtos;
using Microsoft.AspNetCore.SignalR;

namespace IID.Infrastructure.Realtime;

public sealed class SignalRVehicleHubNotifier(IHubContext<InventoryHub, IInventoryClient> hub) : IVehicleHubNotifier
{
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

    private static string? DealershipGroup(Guid dealershipId)
        => dealershipId != Guid.Empty ? InventoryPolicy.GetDealershipGroup(dealershipId) : null;

    public Task VehicleAddedAsync(Vehicle v, CancellationToken ct)
    {
        var group = DealershipGroup(v.DealershipId);
        if (group is null) return Task.CompletedTask;
        return hub.Clients.Group(group).VehicleAdded(MapVehicle(v));
    }

    public Task VehicleUpdatedAsync(Vehicle v, CancellationToken ct)
    {
        var group = DealershipGroup(v.DealershipId);
        if (group is null) return Task.CompletedTask;
        return hub.Clients.Group(group).VehicleUpdated(MapVehicle(v));
    }

    public Task VehicleRemovedAsync(Guid vehicleId, CancellationToken ct)
        => Task.CompletedTask; // vehicleId alone has no dealershipId context — caller should use VehicleUpdatedAsync before delete

    public Task VehicleAgingAsync(Vehicle v, CancellationToken ct)
    {
        var group = DealershipGroup(v.DealershipId);
        if (group is null) return Task.CompletedTask;
        return hub.Clients.Group(group).VehicleAging(MapVehicle(v));
    }

    public Task VehicleTransferredAsync(Vehicle v, Guid sourceDealershipId, CancellationToken ct)
    {
        var resp = MapVehicle(v);
        var targetGroup = DealershipGroup(v.DealershipId);          // new dealership (B)
        var sourceGroup = DealershipGroup(sourceDealershipId);      // old dealership (A)

        return (targetGroup, sourceGroup) switch
        {
            (not null, not null) when targetGroup != sourceGroup =>
                Task.WhenAll(
                    hub.Clients.Group(sourceGroup).VehicleUpdated(resp),  // A sees the vehicle leave
                    hub.Clients.Group(targetGroup).VehicleUpdated(resp)), // B sees the vehicle arrive
            (not null, _) => hub.Clients.Group(targetGroup).VehicleUpdated(resp),
            (_, not null) => hub.Clients.Group(sourceGroup).VehicleUpdated(resp),
            _ => Task.CompletedTask
        };
    }

    public Task VehicleActionLoggedAsync(VehicleAction a, Vehicle? v, CancellationToken ct)
    {
        var vehicleName = v is not null ? $"{v.Year} {v.Make} {v.Model}" : null;
        var resp = new VehicleActionResponse(a.Id, a.VehicleId, a.ActionType.ToString(), a.Notes, a.LoggedAt, vehicleName);
        var group = v is not null ? DealershipGroup(v.DealershipId) : null;
        if (group is null) return Task.CompletedTask;
        return hub.Clients.Group(group).VehicleActionLogged(resp);
    }

    public Task VehicleActionLoggedAsync(VehicleAction a, CancellationToken ct)
        => VehicleActionLoggedAsync(a, null, ct);

    public Task DashboardSummaryUpdatedAsync(DashboardSummaryDto summary, CancellationToken ct)
        => Task.CompletedTask;

    public Task DashboardAlertsUpdatedAsync(IReadOnlyList<DashboardAlertDto> alerts, CancellationToken ct)
        => Task.CompletedTask;

    public Task InventoryChangedAsync(CancellationToken ct)
        => Task.CompletedTask;
}
