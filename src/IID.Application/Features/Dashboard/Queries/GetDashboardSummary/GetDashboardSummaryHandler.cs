using IID.Application.Common.Interfaces;
using IID.Application.Common.Models;
using IID.Application.Dashboard.Dtos;
using MediatR;
namespace IID.Application.Dashboard.Queries.GetDashboardSummary;

public sealed class GetDashboardSummaryHandler(
    IVehicleRepository vehicles,
    IClock clock,
    ILogger<GetDashboardSummaryHandler> logger) : IRequestHandler<GetDashboardSummaryQuery, Result<DashboardSummaryDto>>
{
    public async Task<Result<DashboardSummaryDto>> Handle(GetDashboardSummaryQuery q, CancellationToken ct)
    {
        var asOf = q.AsOfUtc ?? clock.UtcNow;

        // Pull a reasonable page (top 1000) for analytics. For very large fleets
        // this would move to a dedicated reporting view / materialized table.
        const int analyticsPageSize = 1000;
        var (items, _) = await vehicles.ListAsync(
            new VehicleListFilter(Limit: analyticsPageSize, Sort: "dateAdded", Order: "desc"), ct);

        var analytics = new VehicleAnalyticsService(asOf);

        var available = items.Count(v => v.Status == VehicleStatus.Available);
        var pending = items.Count(v => v.Status == VehicleStatus.Pending);
        var sold = items.Count(v => v.Status == VehicleStatus.Sold);
        var wholesale = items.Count(v => v.Status == VehicleStatus.Wholesale);
        var agingCount = items.Count(analytics.IsAging);

        var agingBuckets = analytics.BucketByAging(items);
        var agingBySeverity = agingBuckets.ToDictionary(
            kv => kv.Key.ToString(), kv => kv.Value);

        var totalValue = items.Sum(v => v.AskingPrice.Amount);
        var avgPrice = items.Count == 0 ? 0m : Math.Round(totalValue / items.Count, 2);
        var avgDays = items.Count == 0 ? 0 : (int)items.Average(v => (double)analytics.DaysInInventory(v));

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

        var summary = new DashboardSummaryDto(
            asOf,
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

        logger.LogDebug("Dashboard summary generated: total={Total}", items.Count);

        return Result<DashboardSummaryDto>.Success(summary);
    }
}
