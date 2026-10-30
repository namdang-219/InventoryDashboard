using System.Security.Cryptography;
using System.Text;
using IID.Application.Common.Interfaces;
using IID.Application.Dashboard.Dtos;
using IID.Application.Dashboard.Queries.Dtos;
using IID.Domain.Dashboard;

namespace IID.Application.Features.Dashboard.Services;

/// <summary>
/// Single canonical calculator for precomputed dashboard snapshots across Global and per-Dealership scopes.
/// Consolidates KPIs, quick stats, chart distributions, action-center items, low-inventory alerts,
/// and realtime aging alerts over unbounded scope vehicle sets.
/// </summary>
public sealed class DashboardSnapshotCalculator(
    IVehicleRepository vehicles,
    IClock clock) : IDashboardSnapshotCalculator
{
    public const int LowInventoryThreshold = 3;

    public async Task<DashboardSnapshotPayload> CalculateForScopeAsync(
        Guid? dealershipId,
        DateTimeOffset? asOfUtc = null,
        CancellationToken ct = default)
    {
        var normalizedDealershipId = dealershipId.HasValue && dealershipId.Value != Guid.Empty
            ? dealershipId
            : null;

        var items = await vehicles.ListAllForScopeAsync(normalizedDealershipId, ct);
        return Calculate(normalizedDealershipId, items, asOfUtc ?? clock.UtcNow);
    }

    public DashboardSnapshotPayload Calculate(
        Guid? dealershipId,
        IReadOnlyList<Vehicle> all,
        DateTimeOffset asOfUtc)
    {
        var normalizedDealershipId = dealershipId.HasValue && dealershipId.Value != Guid.Empty
            ? dealershipId
            : null;
        var scopeKey = DashboardSnapshot.BuildScopeKey(normalizedDealershipId);
        var analytics = new VehicleAnalyticsService(asOfUtc);

        // ── 1. Summary (active stock = Status != Sold for valuation & aging) ──
        var available = all.Count(v => v.Status == VehicleStatus.Available);
        var pending = all.Count(v => v.Status == VehicleStatus.Pending);
        var sold = all.Count(v => v.Status == VehicleStatus.Sold);
        var wholesale = all.Count(v => v.Status == VehicleStatus.Wholesale);
        var active = all.Where(v => v.Status != VehicleStatus.Sold).ToList();

        var activeInventoryValue = active.Sum(v => v.AskingPrice.Amount);
        var avgAskingPrice = active.Count == 0 ? 0m : Math.Round(activeInventoryValue / active.Count, 2);
        var avgDaysOnLot = active.Count == 0 ? 0 : (int)active.Average(v => (double)analytics.DaysInInventory(v));
        var agingCount = active.Count(analytics.IsAging);

        var agingBuckets = analytics.BucketByAging(active);
        var agingBySeverity = agingBuckets.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value);

        var topMakes = all
            .GroupBy(v => v.Make)
            .Select(g => new MakeDistributionDto(g.Key, g.Count(), g.Sum(v => v.AskingPrice.Amount)))
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Make, StringComparer.Ordinal)
            .Take(5)
            .ToList();

        var fuelMix = all
            .GroupBy(v => v.FuelType.ToString())
            .Select(g => new FuelTypeDistributionDto(g.Key, g.Count()))
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.FuelType, StringComparer.Ordinal)
            .ToList();

        var demandDistribution = all
            .GroupBy(v => analytics.GetDemandLevel(v).ToString())
            .Select(g => new DemandLevelDto(g.Key, g.Count()))
            .ToList();

        var summary = new DashboardSummaryDto(
            asOfUtc,
            all.Count,
            available,
            pending,
            sold,
            wholesale,
            agingCount,
            agingBySeverity,
            Math.Round(activeInventoryValue, 2),
            avgAskingPrice,
            avgDaysOnLot,
            topMakes,
            fuelMix,
            demandDistribution);

        // ── 2. Quick stats ────────────────────────────────────────────────────
        var quickStats = new QuickStatsDto(
        [
            new("Total Inventory", all.Count.ToString(), null, null, "inventory_2", "primary"),
            new("Available", available.ToString(), null, null, "check_circle", "success"),
            new("Aging (>90d)", agingCount.ToString(), null, null, "warning", "warning"),
            new("Avg Days on Lot", avgDaysOnLot.ToString(), null, null, "schedule", "primary")
        ]);

        // ── 3. Charts ─────────────────────────────────────────────────────────
        var statusBreakdown = all
            .GroupBy(v => v.Status.ToString())
            .Select(g => new StatusBreakdownDto(g.Key, g.Count()))
            .ToList();

        var fuelBreakdown = all
            .GroupBy(v => v.FuelType.ToString())
            .Select(g => new FuelBreakdownDto(g.Key, g.Count()))
            .ToList();

        var ageGroups = new Dictionary<string, int>
        {
            ["0–30 days"] = 0,
            ["31–60 days"] = 0,
            ["61–90 days"] = 0,
            ["91–180 days"] = 0,
            ["180+ days"] = 0
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

        var monthlySales = new List<MonthlySalesDto>(12);
        for (var i = 11; i >= 0; i--)
        {
            var targetMonth = asOfUtc.AddMonths(-i);
            var year = targetMonth.Year;
            var month = targetMonth.Month;
            var label = targetMonth.ToString("MMM yyyy");

            var inMonth = all.Where(v => v.Status == VehicleStatus.Sold
                && v.SoldAt.HasValue
                && v.SoldAt.Value.Year == year
                && v.SoldAt.Value.Month == month).ToList();

            var count = inMonth.Count;
            var revenue = inMonth.Where(v => v.SoldPrice is not null).Sum(v => v.SoldPrice!.Amount);
            monthlySales.Add(new MonthlySalesDto(label, count, revenue));
        }

        var charts = new InventoryChartsDto(statusBreakdown, fuelBreakdown, agingHistogram, monthlySales);

        // ── 4. Action center ──────────────────────────────────────────────────
        var actions = new List<ActionCenterItemDto>();
        foreach (var v in active)
        {
            var demandScore = analytics.GetDemandScore(v);
            var daysOnLot = analytics.DaysInInventory(v);
            var severity = analytics.GetAgingSeverity(v);

            if (severity >= AgingSeverity.Critical)
            {
                actions.Add(new ActionCenterItemDto(
                    v.Id,
                    "aging",
                    $"Critical aging: {v.Year} {v.Make} {v.Model}",
                    $"{daysOnLot} days on lot — consider price adjustment or wholesale.",
                    v.Id,
                    "critical",
                    demandScore,
                    daysOnLot));
            }
            else if (severity >= AgingSeverity.High)
            {
                actions.Add(new ActionCenterItemDto(
                    v.Id,
                    "aging",
                    $"High aging: {v.Year} {v.Make} {v.Model}",
                    $"{daysOnLot} days on lot.",
                    v.Id,
                    "warning",
                    demandScore,
                    daysOnLot));
            }

            if (analytics.GetDemandLevel(v) == DemandLevel.Low)
            {
                actions.Add(new ActionCenterItemDto(
                    v.Id,
                    "demand",
                    $"Low demand: {v.Year} {v.Make} {v.Model}",
                    $"Demand score {demandScore}/100.",
                    v.Id,
                    "info",
                    demandScore,
                    daysOnLot));
            }
        }

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

        // ── 5. Low inventory alerts ───────────────────────────────────────────
        var lowInventoryAlerts = all
            .Where(v => v.Status == VehicleStatus.Available)
            .GroupBy(v => (v.Make, v.Model))
            .Where(g => g.Count() < LowInventoryThreshold)
            .Select(g => new LowInventoryAlertDto(
                MakeModelAlertId(g.Key.Make, g.Key.Model),
                g.Key.Make,
                g.Key.Model,
                g.Count(),
                LowInventoryThreshold))
            .OrderByDescending(a => LowInventoryThreshold - a.Available)
            .ThenBy(a => a.Make, StringComparer.Ordinal)
            .ThenBy(a => a.Model, StringComparer.Ordinal)
            .Take(10)
            .ToList();

        // ── 6. Realtime aging alerts ──────────────────────────────────────────
        var realtimeAlerts = all
            .Where(v => v.Status == VehicleStatus.Available)
            .Select(v => (Vehicle: v, Severity: v.GetAgingSeverity(asOfUtc), Days: v.DaysInInventory(asOfUtc)))
            .Where(x => x.Severity >= AgingSeverity.Warning)
            .OrderByDescending(x => x.Severity)
            .ThenByDescending(x => x.Days)
            .Take(10)
            .Select(x =>
            {
                var alertMsg = x.Severity switch
                {
                    AgingSeverity.Critical => $"{x.Vehicle.Make} has been in stock for {x.Days} days (CRITICAL)",
                    AgingSeverity.High => $"{x.Vehicle.Make} has been in stock for {x.Days} days (HIGH)",
                    _ => $"{x.Vehicle.Make} has been in stock for {x.Days} days"
                };
                return new DashboardAlertDto(x.Vehicle.Id, alertMsg, x.Severity.ToString(), asOfUtc);
            })
            .ToList();

        return new DashboardSnapshotPayload(
            normalizedDealershipId,
            scopeKey,
            summary,
            quickStats,
            charts,
            actionCenter,
            [],
            lowInventoryAlerts,
            realtimeAlerts,
            asOfUtc);
    }

    public static Guid MakeModelAlertId(string make, string model)
    {
        var seed = $"iid:low-inventory-alert:{make}:{model}";
        var bytes = Encoding.UTF8.GetBytes(seed);
        var hash = MD5.HashData(bytes);
        return new Guid(hash);
    }
}

