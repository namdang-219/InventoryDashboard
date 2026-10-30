using IID.Application.Dashboard.Dtos;

namespace IID.Application.Common.Messaging;

public static class KafkaTopics
{
    public const string VehicleMutations = "iid.vehicle-mutations";
    public const string DashboardUpdates = "iid.dashboard-updates";
}

public static class VehicleMutationTypes
{
    public const string Added = "Added";
    public const string Updated = "Updated";
    public const string StatusChanged = "StatusChanged";
    public const string Sold = "Sold";
    public const string Transferred = "Transferred";
    public const string Removed = "Removed";
    public const string DealershipCreated = "DealershipCreated";
    public const string Seeded = "Seeded";
}

public sealed record VehicleMutatedIntegrationEvent(
    Guid EventId,
    string MutationType,
    Guid? VehicleId,
    Guid? DealershipId,
    Guid? PreviousDealershipId,
    DateTimeOffset OccurredAtUtc);

public sealed record DashboardSnapshotUpdatedIntegrationEvent(
    Guid EventId,
    string ScopeKey,
    Guid? DealershipId,
    DashboardSummaryDto Summary,
    IReadOnlyList<DashboardAlertDto> Alerts,
    DateTimeOffset ComputedAtUtc,
    long Version);

public interface IKafkaEventProducer
{
    Task PublishAsync<TMessage>(string topic, string key, TMessage message, CancellationToken ct = default);
}
