using IID.Application.Common.Interfaces;
using IID.Application.Common.Models;
using IID.Application.Dashboard.Dtos;
using IID.Application.Dashboard.Queries.Dtos;
using IID.Application.Features.Dashboard.Services;
using IID.Application.Logging;
using IID.Domain.Dashboard;
using MediatR;

namespace IID.Application.Dashboard.Queries.GetDashboardBundle;

public sealed class GetDashboardBundleHandler(
    IDashboardSnapshotRepository snapshots,
    IDashboardSnapshotCalculator calculator,
    IVehicleRepository vehicles,
    IClock clock,
    ILogger<GetDashboardBundleHandler> logger) : IRequestHandler<GetDashboardBundleQuery, Result<DashboardBundleDto>>
{
    public async Task<Result<DashboardBundleDto>> Handle(GetDashboardBundleQuery q, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var page = Math.Max(1, q.Page);
        var pageSize = Math.Clamp(q.PageSize, 1, 100);
        var dealershipId = q.DealershipId.HasValue && q.DealershipId.Value != Guid.Empty
            ? q.DealershipId
            : null;

        var scopeKey = DashboardSnapshot.BuildScopeKey(dealershipId);
        var snapshot = await snapshots.GetByScopeAsync(scopeKey, ct);

        DashboardSummaryDto summary;
        QuickStatsDto quickStats;
        InventoryChartsDto charts;
        IReadOnlyList<ActionCenterItemDto> actionCenter;
        IReadOnlyList<AiInsightDto> aiInsights;

        if (snapshot is not null)
        {
            summary = DashboardSnapshotSerializer.ReadSummary(snapshot);
            quickStats = DashboardSnapshotSerializer.ReadQuickStats(snapshot);
            charts = DashboardSnapshotSerializer.ReadCharts(snapshot);
            actionCenter = DashboardSnapshotSerializer.ReadActionCenter(snapshot);
            aiInsights = DashboardSnapshotSerializer.ReadAiInsights(snapshot);
        }
        else
        {
            logger.LogWarning(
                "Static dashboard snapshot not found for scope {ScopeKey}; computing on-demand fallback.",
                scopeKey);

            var fallback = await calculator.CalculateForScopeAsync(dealershipId, now, ct);
            summary = fallback.Summary;
            quickStats = fallback.QuickStats;
            charts = fallback.Charts;
            actionCenter = fallback.ActionCenter;
            aiInsights = fallback.AiInsights;
        }

        var (inventoryItems, inventoryTotal) = await vehicles.ListAsync(
            new VehicleListFilter(
                Page: page,
                Limit: pageSize,
                Sort: "dateAdded",
                Order: "desc",
                DealershipId: dealershipId),
            ct);

        var analytics = new VehicleAnalyticsService(now);
        var inventory = new DashboardInventoryDto(
            inventoryItems.Select(v => new DashboardVehicleDto(
                v.Id,
                v.Vin.Value,
                v.StockNumber ?? string.Empty,
                v.Make,
                v.Model,
                v.Year,
                v.Color,
                v.Mileage,
                v.FuelType.ToString(),
                v.AskingPrice.Amount,
                v.AskingPrice.Currency,
                v.Status.ToString(),
                v.DateAddedToInventory,
                analytics.DaysInInventory(v),
                analytics.IsAging(v),
                analytics.GetAgingSeverity(v).ToString(),
                analytics.GetDemandLevel(v).ToString(),
                analytics.GetDemandScore(v))).ToList(),
            page,
            pageSize,
            inventoryTotal);

        var bundle = new DashboardBundleDto(summary, quickStats, charts, actionCenter, aiInsights, inventory);

        logger.DashboardBundleGenerated(summary.TotalInventory, actionCenter.Count, page);

        return Result<DashboardBundleDto>.Success(bundle);
    }
}
