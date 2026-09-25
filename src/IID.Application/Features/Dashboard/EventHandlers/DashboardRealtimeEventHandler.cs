using IID.Application.Common.Interfaces;
using IID.Domain.Vehicles.Events;
using MediatR;

namespace IID.Application.Features.Dashboard.EventHandlers;

public sealed class DashboardRealtimeEventHandler(
    IDashboardService dashboardService,
    IVehicleHubNotifier hubNotifier) :
    INotificationHandler<VehicleAdded>,
    INotificationHandler<VehicleStatusChanged>,
    INotificationHandler<VehicleSold>,
    INotificationHandler<VehicleRemoved>,
    INotificationHandler<VehicleUpdated>
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
