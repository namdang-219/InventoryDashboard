using IID.Application.Common.Interfaces;
using IID.Application.Common.Models;
using IID.Application.Dashboard.Dtos;

namespace IID.Application.Features.Dashboard.Services;

public sealed class DashboardService : IDashboardService
{
    private readonly IVehicleRepository _vehicles;
    private readonly IClock _clock;

    public DashboardService(IVehicleRepository vehicles, IClock clock)
    {
        _vehicles = vehicles;
        _clock = clock;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var (items, _) = await _vehicles.ListAsync(
            new VehicleListFilter(Limit: 1000, Sort: "dateAdded", Order: "desc"), cancellationToken);

        var analytics = new VehicleAnalyticsService(now);

        var available = items.Count(v => v.Status == VehicleStatus.Available);
        var pending = items.Count(v => v.Status == VehicleStatus.Pending);
        var sold = items.Count(v => v.Status == VehicleStatus.Sold);
        var wholesale = items.Count(v => v.Status == VehicleStatus.Wholesale);
        var agingCount = items.Count(analytics.IsAging);
        var totalValue = items.Sum(v => v.AskingPrice.Amount);
        var avgPrice = items.Count == 0 ? 0m : Math.Round(totalValue / items.Count, 2);
        var avgDays = items.Count == 0 ? 0 : (int)items.Average(v => (double)analytics.DaysInInventory(v));

        var agingBuckets = analytics.BucketByAging(items);
        var agingBySeverity = agingBuckets.ToDictionary(
            kv => kv.Key.ToString(), kv => kv.Value);

        var topMakes = items
            .GroupBy(v => v.Make)
            .Select(g => new MakeDistributionDto(g.Key, g.Count(), g.Sum(v => v.AskingPrice.Amount)))
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToList();

        var fuelMix = items
            .GroupBy(v => v.FuelType.ToString())
            .Select(g => new FuelTypeDistributionDto(g.Key, g.Count()))
            .OrderByDescending(x => x.Count)
            .ToList();

        var demandGroups = items
            .GroupBy(v => analytics.GetDemandLevel(v).ToString())
            .Select(g => new DemandLevelDto(g.Key, g.Count()))
            .ToList();

        return new DashboardSummaryDto(
            now,
            items.Count,
            available,
            pending,
            sold,
            wholesale,
            agingCount,
            agingBySeverity,
            Math.Round(totalValue, 2),
            avgPrice,
            avgDays,
            topMakes,
            fuelMix,
            demandGroups);
    }

    public async Task<IReadOnlyList<DashboardAlertDto>> GetAlertsAsync(CancellationToken cancellationToken)
    {
        var (items, _) = await _vehicles.ListAsync(
            new VehicleListFilter(Limit: 1000, Sort: "dateAdded", Order: "desc"), cancellationToken);

        var alerts = new List<DashboardAlertDto>();
        var now = _clock.UtcNow;

        foreach (var v in items.Where(v => v.Status == VehicleStatus.Available))
        {
            var severity = v.GetAgingSeverity(now);
            if (severity >= AgingSeverity.Warning)
            {
                var alertMsg = severity switch
                {
                    AgingSeverity.Critical => $"{v.Make} has been in stock for {v.DaysInInventory(now)} days (CRITICAL)",
                    AgingSeverity.High => $"{v.Make} has been in stock for {v.DaysInInventory(now)} days (HIGH)",
                    _ => $"{v.Make} has been in stock for {v.DaysInInventory(now)} days"
                };

                alerts.Add(new DashboardAlertDto(v.Id, alertMsg, severity.ToString(), now));
            }
        }

        return alerts.OrderByDescending(a => a.CreatedAtUtc).Take(10).ToList();
    }
}
