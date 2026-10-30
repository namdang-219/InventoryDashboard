using IID.Domain.Common;

namespace IID.Domain.Dashboard;

/// <summary>
/// Precomputed static dashboard snapshot persisted per scope ("global" or "dealership:{id}").
/// Maintained asynchronously by IID.ReportGenerator and served by IID.Api in O(1).
/// </summary>
public sealed class DashboardSnapshot : Entity
{
    public const string GlobalScopeKey = "global";

    public string ScopeKey { get; private set; } = GlobalScopeKey;
    public Guid? DealershipId { get; private set; }
    public string SummaryJson { get; private set; } = string.Empty;
    public string QuickStatsJson { get; private set; } = string.Empty;
    public string ChartsJson { get; private set; } = string.Empty;
    public string ActionCenterJson { get; private set; } = string.Empty;
    public string AiInsightsJson { get; private set; } = string.Empty;
    public string LowInventoryAlertsJson { get; private set; } = string.Empty;
    public string RealtimeAlertsJson { get; private set; } = string.Empty;
    public DateTimeOffset ComputedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
    public long Version { get; private set; } = 1;

    private DashboardSnapshot() { }

    public static string BuildScopeKey(Guid? dealershipId)
        => dealershipId.HasValue && dealershipId.Value != Guid.Empty
            ? $"dealership:{dealershipId.Value:D}"
            : GlobalScopeKey;

    public static DashboardSnapshot Create(
        Guid? dealershipId,
        string summaryJson,
        string quickStatsJson,
        string chartsJson,
        string actionCenterJson,
        string aiInsightsJson,
        string lowInventoryAlertsJson,
        string realtimeAlertsJson,
        DateTimeOffset computedAtUtc)
    {
        var normalizedDealershipId = dealershipId.HasValue && dealershipId.Value != Guid.Empty
            ? dealershipId
            : null;

        return new DashboardSnapshot
        {
            Id = Guid.NewGuid(),
            ScopeKey = BuildScopeKey(normalizedDealershipId),
            DealershipId = normalizedDealershipId,
            SummaryJson = summaryJson,
            QuickStatsJson = quickStatsJson,
            ChartsJson = chartsJson,
            ActionCenterJson = actionCenterJson,
            AiInsightsJson = aiInsightsJson,
            LowInventoryAlertsJson = lowInventoryAlertsJson,
            RealtimeAlertsJson = realtimeAlertsJson,
            ComputedAtUtc = computedAtUtc,
            Version = 1
        };
    }

    public void Update(
        string summaryJson,
        string quickStatsJson,
        string chartsJson,
        string actionCenterJson,
        string aiInsightsJson,
        string lowInventoryAlertsJson,
        string realtimeAlertsJson,
        DateTimeOffset computedAtUtc)
    {
        SummaryJson = summaryJson;
        QuickStatsJson = quickStatsJson;
        ChartsJson = chartsJson;
        ActionCenterJson = actionCenterJson;
        AiInsightsJson = aiInsightsJson;
        LowInventoryAlertsJson = lowInventoryAlertsJson;
        RealtimeAlertsJson = realtimeAlertsJson;
        ComputedAtUtc = computedAtUtc;
        Version++;
    }
}

