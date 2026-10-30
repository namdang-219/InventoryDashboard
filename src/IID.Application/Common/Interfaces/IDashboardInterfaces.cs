using IID.Application.Dashboard.Dtos;
using IID.Application.Dashboard.Queries.Dtos;
using IID.Domain.Dashboard;

namespace IID.Application.Common.Interfaces;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken ct);
    Task<IReadOnlyList<DashboardAlertDto>> GetAlertsAsync(CancellationToken ct);
}

public sealed record DashboardSnapshotPayload(
    Guid? DealershipId,
    string ScopeKey,
    DashboardSummaryDto Summary,
    QuickStatsDto QuickStats,
    InventoryChartsDto Charts,
    IReadOnlyList<ActionCenterItemDto> ActionCenter,
    IReadOnlyList<AiInsightDto> AiInsights,
    IReadOnlyList<LowInventoryAlertDto> LowInventoryAlerts,
    IReadOnlyList<DashboardAlertDto> RealtimeAlerts,
    DateTimeOffset ComputedAtUtc);

public interface IDashboardSnapshotCalculator
{
    DashboardSnapshotPayload Calculate(Guid? dealershipId, IReadOnlyList<Vehicle> vehicles, DateTimeOffset asOfUtc);
    Task<DashboardSnapshotPayload> CalculateForScopeAsync(Guid? dealershipId, DateTimeOffset? asOfUtc = null, CancellationToken ct = default);
}

public interface IDashboardSnapshotRepository
{
    Task<DashboardSnapshot?> GetByScopeAsync(string scopeKey, CancellationToken ct = default);
    Task<DashboardSnapshot> UpsertAsync(DashboardSnapshotPayload payload, CancellationToken ct = default);
}

public interface IDashboardSnapshotRefresher
{
    Task<int> RefreshAllSnapshotsAsync(bool publishUpdates = false, CancellationToken ct = default);
    Task<int> RefreshForMutationsAsync(
        IReadOnlyCollection<IID.Application.Common.Messaging.VehicleMutatedIntegrationEvent> mutations,
        CancellationToken ct = default);
}
