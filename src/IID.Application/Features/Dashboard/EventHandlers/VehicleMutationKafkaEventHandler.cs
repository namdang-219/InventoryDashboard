using IID.Application.Common.Interfaces;
using IID.Application.Common.Messaging;
using IID.Domain.Dashboard;
using IID.Domain.Dealerships.Events;
using IID.Domain.Vehicles.Events;

namespace IID.Application.Features.Dashboard.EventHandlers;

/// <summary>
/// Publishes vehicle and dealership lifecycle mutations directly to Apache Kafka post-commit
/// so <c>IID.ReportGenerator</c> can asynchronously regenerate affected static dashboard snapshots.
/// </summary>
public sealed class VehicleMutationKafkaEventHandler(
    IKafkaEventProducer producer,
    ILogger<VehicleMutationKafkaEventHandler> logger) :
    IDomainEventHandler<VehicleAdded>,
    IDomainEventHandler<VehicleUpdated>,
    IDomainEventHandler<VehicleStatusChanged>,
    IDomainEventHandler<VehicleSold>,
    IDomainEventHandler<VehicleRemoved>,
    IDomainEventHandler<VehicleTransferred>,
    IDomainEventHandler<DealershipAdded>
{
    public Task Handle(VehicleAdded notification, CancellationToken cancellationToken)
        => PublishMutationAsync(
            VehicleMutationTypes.Added,
            notification.Vehicle.Id,
            notification.Vehicle.DealershipId,
            previousDealershipId: null,
            notification.OccurredOnUtc,
            cancellationToken);

    public Task Handle(VehicleUpdated notification, CancellationToken cancellationToken)
        => PublishMutationAsync(
            VehicleMutationTypes.Updated,
            notification.Vehicle.Id,
            notification.Vehicle.DealershipId,
            previousDealershipId: null,
            notification.OccurredOnUtc,
            cancellationToken);

    public Task Handle(VehicleStatusChanged notification, CancellationToken cancellationToken)
        => PublishMutationAsync(
            VehicleMutationTypes.StatusChanged,
            notification.Vehicle.Id,
            notification.Vehicle.DealershipId,
            previousDealershipId: null,
            notification.OccurredOnUtc,
            cancellationToken);

    public Task Handle(VehicleSold notification, CancellationToken cancellationToken)
        => PublishMutationAsync(
            VehicleMutationTypes.Sold,
            notification.Vehicle.Id,
            notification.Vehicle.DealershipId,
            previousDealershipId: null,
            notification.OccurredOnUtc,
            cancellationToken);

    public Task Handle(VehicleRemoved notification, CancellationToken cancellationToken)
        => PublishMutationAsync(
            VehicleMutationTypes.Removed,
            notification.Vehicle.Id,
            notification.Vehicle.DealershipId,
            previousDealershipId: null,
            notification.OccurredOnUtc,
            cancellationToken);

    public Task Handle(VehicleTransferred notification, CancellationToken cancellationToken)
        => PublishMutationAsync(
            VehicleMutationTypes.Transferred,
            notification.Vehicle.Id,
            notification.NewDealershipId,
            notification.PreviousDealershipId,
            notification.OccurredOnUtc,
            cancellationToken);

    public Task Handle(DealershipAdded notification, CancellationToken cancellationToken)
        => PublishMutationAsync(
            VehicleMutationTypes.DealershipCreated,
            vehicleId: null,
            notification.Dealership.Id,
            previousDealershipId: null,
            notification.OccurredOnUtc,
            cancellationToken);

    private async Task PublishMutationAsync(
        string mutationType,
        Guid? vehicleId,
        Guid? dealershipId,
        Guid? previousDealershipId,
        DateTimeOffset occurredAtUtc,
        CancellationToken ct)
    {
        var normalizedDealershipId = dealershipId.HasValue && dealershipId.Value != Guid.Empty
            ? dealershipId
            : null;
        var normalizedPreviousDealershipId = previousDealershipId.HasValue && previousDealershipId.Value != Guid.Empty
            ? previousDealershipId
            : null;

        var key = normalizedDealershipId?.ToString("D") ?? DashboardSnapshot.GlobalScopeKey;
        var message = new VehicleMutatedIntegrationEvent(
            Guid.NewGuid(),
            mutationType,
            vehicleId,
            normalizedDealershipId,
            normalizedPreviousDealershipId,
            occurredAtUtc);

        try
        {
            await producer.PublishAsync(KafkaTopics.VehicleMutations, key, message, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // DomainEventDispatchInterceptor runs inside SavedChangesAsync after DB commit;
            // log and avoid failing the already-committed HTTP command if Kafka is transiently unavailable.
            logger.LogError(
                ex,
                "Failed to publish {MutationType} event to Kafka for vehicle={VehicleId}, dealership={DealershipId}.",
                mutationType,
                vehicleId,
                normalizedDealershipId);
        }
    }
}

