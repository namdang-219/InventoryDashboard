using IID.Application.Common.Interfaces;
using IID.Domain.Vehicles.Events;
using MediatR;

namespace IID.Application.Features.Dashboard.EventHandlers;

/// <summary>
/// Broadcasts dashboard-level refresh on aggregate-level domain events.
///
/// Note: <see cref="VehicleTransferred"/> is intentionally NOT handled here.
/// It is an entity-level event broadcast once by
/// <c>TransferDealershipHandler</c> via <see cref="IVehicleHubNotifier.VehicleTransferredAsync"/>,
/// to the source + target dealership groups only. Adding it here would
/// trigger a redundant global dashboard refresh on every transfer.
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
