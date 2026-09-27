using IID.Application.Common.Interfaces;
using IID.Infrastructure.Realtime.Contracts;
using Microsoft.AspNetCore.SignalR;

namespace IID.Infrastructure.Realtime;

public sealed class SignalRVehicleHubNotifier(
    IHubContext<InventoryHub, IInventoryClient> hub,
    IRealtimeContractMapper mapper) : IVehicleHubNotifier
{
    private static string? DealershipGroup(Guid dealershipId)
        => dealershipId != Guid.Empty ? InventoryHubConstants.DealershipGroup(dealershipId) : null;

    public Task VehicleAddedAsync(Vehicle v, CancellationToken ct)
    {
        var group = DealershipGroup(v.DealershipId);
        if (group is null) return Task.CompletedTask;
        return hub.Clients.Group(group).VehicleAdded(mapper.MapVehicle(v));
    }

    public Task VehicleUpdatedAsync(Vehicle v, CancellationToken ct)
    {
        var group = DealershipGroup(v.DealershipId);
        if (group is null) return Task.CompletedTask;
        return hub.Clients.Group(group).VehicleUpdated(mapper.MapVehicle(v));
    }

    public Task VehicleRemovedAsync(Guid vehicleId, CancellationToken ct)
        => hub.Clients.All.VehicleRemoved(vehicleId);

    public Task VehicleAgingAsync(Vehicle v, CancellationToken ct)
    {
        var group = DealershipGroup(v.DealershipId);
        if (group is null) return Task.CompletedTask;
        return hub.Clients.Group(group).VehicleAging(mapper.MapVehicle(v));
    }

    public Task VehicleTransferredAsync(Vehicle v, Guid sourceDealershipId, CancellationToken ct)
    {
        var resp = mapper.MapVehicle(v);
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
        var resp = mapper.MapVehicleAction(a, v);
        var group = v is not null ? DealershipGroup(v.DealershipId) : null;
        if (group is null) return Task.CompletedTask;
        return hub.Clients.Group(group).VehicleActionLogged(resp);
    }

    public Task VehicleActionLoggedAsync(VehicleAction a, CancellationToken ct)
        => VehicleActionLoggedAsync(a, null, ct);

    public Task DashboardSummaryUpdatedAsync(IID.Application.Dashboard.Dtos.DashboardSummaryDto summary, CancellationToken ct)
        => hub.Clients.All.DashboardSummaryUpdated(mapper.MapSummary(summary));

    public Task DashboardAlertsUpdatedAsync(IReadOnlyList<IID.Application.Dashboard.Dtos.DashboardAlertDto> alerts, CancellationToken ct)
        => hub.Clients.All.DashboardAlertsUpdated(alerts.Select(mapper.MapAlert).ToList());

    public Task InventoryChangedAsync(CancellationToken ct)
        => hub.Clients.All.InventoryChanged();
}
