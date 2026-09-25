using IID.Application.Common.Interfaces;
using IID.Application.Common.Models;
using IID.Application.Dashboard.Dtos;
using IID.Application.Dashboard.Queries.Dtos;
using IID.Application.Vehicles.Queries.Dtos;
using MediatR;
namespace IID.Application.Dashboard.Queries.GetDashboardBundle;

public sealed class GetDashboardBundleHandler(
    IVehicleRepository vehicles,
    IClock clock,
    ILogger<GetDashboardBundleHandler> logger) : IRequestHandler<GetDashboardBundleQuery, Result<DashboardBundleDto>>
{
    public async Task<Result<DashboardBundleDto>> Handle(GetDashboardBundleQuery q, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var page = Math.Max(1, q.Page);
        var pageSize = Math.Clamp(q.PageSize, 1, 100);

        // ── 1. Pull analytics slice ─────────────────────────────────────────
        const int analyticsPageSize = 1000;
        var (all, _) = await vehicles.ListAsync(
            null, null, null, null, null, 1, analyticsPageSize, "dateAdded", "desc", ct, q.DealershipId);

        var analytics = new VehicleAnalyticsService(now);

        // ── 2. Summary ──────────────────────────────────────────────────────
        var available = all.Count(v => v.Status == VehicleStatus.Available);
        var pending = all.Count(v => v.Status == VehicleStatus.Pending);
        var sold = all.Count(v => v.Status == VehicleStatus.Sold);
        var wholesale = all.Count(v => v.Status == VehicleStatus.Wholesale);
        var active = all.Where(v => v.Status != VehicleStatus.Sold).ToList();
        var activeInventory = active.Sum(v => v.AskingPrice.Amount);
        var avgDays = active.Count == 0 ? 0 : (int)active.Average(v => (double)analytics.DaysInInventory(v));

        var agingBuckets = analytics.BucketByAging(active);
        var agingBySeverity = agingBuckets.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value);

        var topMakes = all
            .GroupBy(v => v.Make)
            .Select(g => new MakeDistributionDto(g.Key, g.Count(), g.Sum(v => v.AskingPrice.Amount)))
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToList();

        var fuelMix = all
            .GroupBy(v => v.FuelType.ToString())
            .Select(g => new FuelTypeDistributionDto(g.Key, g.Count()))
            .OrderByDescending(x => x.Count)
            .ToList();

        var demandDistribution = all
            .GroupBy(v => analytics.GetDemandLevel(v).ToString())
            .Select(g => new DemandLevelDto(g.Key, g.Count()))
            .ToList();

        var summary = new DashboardSummaryDto(
            now,
            all.Count,
            available,
            pending,
            sold,
            wholesale,
            active.Count(a => analytics.IsAging(a)),
            agingBySeverity,
            Math.Round(activeInventory, 2),
            active.Count == 0 ? 0m : Math.Round(activeInventory / active.Count, 2),
            avgDays,
            topMakes,
            fuelMix,
            demandDistribution);

        // ── 3. Quick stats ──────────────────────────────────────────────────
        var quickStats = new QuickStatsDto(new List<QuickStatItemDto>
        {
            new("Total Inventory", all.Count.ToString(), null, null, "inventory_2", "primary"),
            new("Available", available.ToString(), null, null, "check_circle", "success"),
            new("Aging (>90d)", active.Count(a => analytics.IsAging(a)).ToString(), null, null, "warning", "warning"),
            new("Avg Days on Lot", avgDays.ToString(), null, null, "schedule", "primary"),
        });

        // ── 4. Chart data ──────────────────────────────────────────────────
        var statusBreakdown = all
            .GroupBy(v => v.Status.ToString())
            .Select(g => new StatusBreakdownDto(g.Key, g.Count()))
            .ToList();

        var fuelBreakdown = all
            .GroupBy(v => v.FuelType.ToString())
            .Select(g => new FuelBreakdownDto(g.Key, g.Count()))
            .ToList();

        // Age buckets: 0-30d, 31-60d, 61-90d, 91-180d, 181+
        var ageGroups = new Dictionary<string, int>
        {
            ["0–30 days"] = 0, ["31–60 days"] = 0, ["61–90 days"] = 0, ["91–180 days"] = 0, ["180+ days"] = 0
        };
        foreach (var v in active)
        {
            var d = analytics.DaysInInventory(v);
            if (d <= 30) ageGroups["0–30 days"]++;
            else if (d <= 60) ageGroups["31–60 days"]++;
            else if (d <= 90) ageGroups["61–90 days"]++;
            else if (d <= 180) ageGroups["91–180 days"]++;
            else ageGroups["180+ days"]++;
        }
        var agingHistogram = ageGroups.Select(kv => new AgingBucketDto(kv.Key, kv.Value)).ToList();

        // Monthly sales: last 6 months from sold vehicles
        var sixMonthsAgo = now.AddMonths(-6);
        var soldVehicles = all
            .Where(v => v.Status == VehicleStatus.Sold && v.SoldAt.HasValue && v.SoldAt.Value >= sixMonthsAgo)
            .ToList();
        var monthlySales = soldVehicles
            .GroupBy(v => v.SoldAt!.Value.ToString("MMM yyyy"))
            .Select(g => new MonthlySalesDto(g.Key, g.Count(), g.Where(v => v.SoldPrice is not null).Sum(v => v.SoldPrice!.Amount)))
            .OrderBy(x => x.Month)
            .TakeLast(6)
            .ToList();

        var charts = new InventoryChartsDto(statusBreakdown, fuelBreakdown, agingHistogram, monthlySales);

        // ── 5. Action center ─────────────────────────────────────────────────
        var actions = new List<ActionCenterItemDto>();
        foreach (var v in active)
        {
            var demandScore = analytics.GetDemandScore(v);
            var daysOnLot = analytics.DaysInInventory(v);

            var severity = analytics.GetAgingSeverity(v);
            if (severity >= AgingSeverity.Critical)
            {
                actions.Add(new ActionCenterItemDto(
                    v.Id, "aging",
                    $"Critical aging: {v.Year} {v.Make} {v.Model}",
                    $"{daysOnLot} days on lot — consider price adjustment or wholesale.",
                    v.Id, "critical",
                    demandScore,
                    daysOnLot));
            }
            else if (severity >= AgingSeverity.High)
            {
                actions.Add(new ActionCenterItemDto(
                    v.Id, "aging",
                    $"High aging: {v.Year} {v.Make} {v.Model}",
                    $"{daysOnLot} days on lot.",
                    v.Id, "warning",
                    demandScore,
                    daysOnLot));
            }

            var demand = analytics.GetDemandLevel(v);
            if (demand == DemandLevel.Low)
            {
                actions.Add(new ActionCenterItemDto(
                    v.Id, "demand",
                    $"Low demand: {v.Year} {v.Make} {v.Model}",
                    $"Demand score {demandScore}/100.",
                    v.Id, "info",
                    demandScore,
                    daysOnLot));
            }
        }

        // Sort by Priority: Critical (0) -> Warning (1) -> Info (2), then by Demand Score ASC (lowest demand = highest urgency), then DaysOnLot DESC
        var actionCenter = actions
            .OrderBy(a => a.Severity switch
            {
                "critical" => 0,
                "warning" => 1,
                _ => 2
            })
            .ThenBy(a => a.DemandScore ?? 100)
            .ThenByDescending(a => a.DaysOnLot ?? 0)
            .Take(20)
            .ToList();

        // ── 6. AI insights (placeholder) ───────────────────────────────────
        var aiInsights = new List<AiInsightDto>();

        // ── 7. Paginated inventory slice ────────────────────────────────────
        var (inventoryItems, inventoryTotal) = await vehicles.ListAsync(
            null, null, null, null, null, page, pageSize, "dateAdded", "desc", ct, q.DealershipId);

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

        if (logger.IsEnabled(LogLevel.Debug))
            logger.LogDebug("Dashboard bundle: total={Total}, page={Page}", all.Count, page);

        return Result<DashboardBundleDto>.Success(bundle);
    }
}
