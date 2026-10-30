using System.Text.Json;
using IID.Application.Common.Interfaces;
using IID.Application.Dashboard.Dtos;
using IID.Application.Dashboard.Queries.Dtos;
using IID.Domain.Dashboard;

namespace IID.Application.Features.Dashboard.Services;

/// <summary>
/// Canonical JSON serializer/deserializer for <see cref="DashboardSnapshot"/> columns.
/// </summary>
public static class DashboardSnapshotSerializer
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public static string Serialize<T>(T value)
        => JsonSerializer.Serialize(value, JsonOptions);

    public static T Deserialize<T>(string json, Func<T> fallbackFactory)
    {
        if (string.IsNullOrWhiteSpace(json))
            return fallbackFactory();

        return JsonSerializer.Deserialize<T>(json, JsonOptions) ?? fallbackFactory();
    }

    public static DashboardSummaryDto ReadSummary(DashboardSnapshot snapshot)
        => Deserialize(
            snapshot.SummaryJson,
            () => new DashboardSummaryDto(
                snapshot.ComputedAtUtc,
                0, 0, 0, 0, 0, 0,
                new Dictionary<string, int>(),
                0m, 0m, 0,
                [], [], []));

    public static QuickStatsDto ReadQuickStats(DashboardSnapshot snapshot)
        => Deserialize(snapshot.QuickStatsJson, () => new QuickStatsDto([]));

    public static InventoryChartsDto ReadCharts(DashboardSnapshot snapshot)
        => Deserialize(
            snapshot.ChartsJson,
            () => new InventoryChartsDto([], [], [], []));

    public static IReadOnlyList<ActionCenterItemDto> ReadActionCenter(DashboardSnapshot snapshot)
        => Deserialize<List<ActionCenterItemDto>>(snapshot.ActionCenterJson, () => []);

    public static IReadOnlyList<AiInsightDto> ReadAiInsights(DashboardSnapshot snapshot)
        => Deserialize<List<AiInsightDto>>(snapshot.AiInsightsJson, () => []);

    public static IReadOnlyList<LowInventoryAlertDto> ReadLowInventoryAlerts(DashboardSnapshot snapshot)
        => Deserialize<List<LowInventoryAlertDto>>(snapshot.LowInventoryAlertsJson, () => []);

    public static IReadOnlyList<DashboardAlertDto> ReadRealtimeAlerts(DashboardSnapshot snapshot)
        => Deserialize<List<DashboardAlertDto>>(snapshot.RealtimeAlertsJson, () => []);
}

