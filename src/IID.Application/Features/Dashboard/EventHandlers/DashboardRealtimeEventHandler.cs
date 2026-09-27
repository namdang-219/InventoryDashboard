using IID.Application.Common.Interfaces;
using IID.Domain.Vehicles.Events;
using MediatR;

namespace IID.Application.Features.Dashboard.EventHandlers;

/// <summary>
/// Broadcasts dashboard-level refresh on aggregate-level domain events.
/// This handler does NOT route per-vehicle SignalR notifications to dealer
/// groups — that responsibility is the
/// <see cref="Features.Vehicles.EventHandlers.VehicleRealtimeEventHandler"/>
/// and <see cref="Features.Vehicles.EventHandlers.VehicleTransferRealtimeEventHandler"/>.
/// <see cref="Domain.Vehicles.Events.VehicleTransferred"/> is intentionally not
/// handled here because the dashboard does not need to recompute on every dealer
/// transfer (per-row notifications already went out via the transfer broadcaster).
/// </summary>
public sealed class DashboardRealtimeEventHandler(
    IDashboardService dashboardService,
    IVehicleHubNotifier hubNotifier) :
    IDomainEventHandler<VehicleAdded>,
    IDomainEventHandler<VehicleStatusChanged>,
    IDomainEventHandler<VehicleSold>,
    IDomainEventHandler<VehicleRemoved>,
    IDomainEventHandler<VehicleUpdated>
{
    public Task Handle(VehicleAdded notification, CancellationToken cancellationToken)
        => BroadcastUpdateAsync(cancellationToken);

    public Task Handle(VehicleStatusChanged notification, CancellationToken cancellationToken)
        => BroadcastUpdateAsync(cancellationToken);

    public Task Handle(VehicleSold notification, CancellationToken cancellationToken)
        => BroadcastUpdateAsync(cancellationToken);

    public Task Handle(VehicleRemoved notification, CancellationToken cancellationToken)
        => BroadcastUpdateAsync(cancellationToken);

    public Task Handle(VehicleUpdated notification, CancellationToken cancellationToken)
        => BroadcastUpdateAsync(cancellationToken);

    private async Task BroadcastUpdateAsync(CancellationToken cancellationToken)
    {
        var summary = await dashboardService.GetSummaryAsync(cancellationToken);
        var alerts = await dashboardService.GetAlertsAsync(cancellationToken);

        await hubNotifier.DashboardSummaryUpdatedAsync(summary, cancellationToken);
        await hubNotifier.DashboardAlertsUpdatedAsync(alerts.ToList(), cancellationToken);
        await hubNotifier.InventoryChangedAsync(cancellationToken);
    }
}
