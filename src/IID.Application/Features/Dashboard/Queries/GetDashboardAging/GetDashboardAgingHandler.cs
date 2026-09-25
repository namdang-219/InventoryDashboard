using IID.Application.Common.Interfaces;
using IID.Application.Common.Models;
using IID.Application.Dashboard.Queries.Dtos;
using MediatR;
namespace IID.Application.Dashboard.Queries.GetDashboardAging;

public sealed class GetDashboardAgingHandler(
    IVehicleRepository vehicles,
    IClock clock,
    ILogger<GetDashboardAgingHandler> logger) : IRequestHandler<GetDashboardAgingQuery, Result<PagedResult<AgingStockItemDto>>>
{
    public async Task<Result<PagedResult<AgingStockItemDto>>> Handle(GetDashboardAgingQuery q, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var page = Math.Max(1, q.Page);
        var limit = Math.Clamp(q.Limit, 1, 100);

        // Pull a generous page to filter by age server-side
        const int fetchSize = 500;
        var (items, _) = await vehicles.ListAsync(
            new VehicleListFilter(Limit: fetchSize, Sort: "dateAdded", Order: "asc", DealershipId: q.DealershipId), ct);

        var analytics = new VehicleAnalyticsService(now);

        // Filter to aging vehicles and map
        var aging = items
            .Where(v => v.Status != VehicleStatus.Sold && analytics.DaysInInventory(v) >= q.MinAgeDays)
            .OrderByDescending(v => analytics.DaysInInventory(v))
            .Select(v => new AgingStockItemDto(
                v.Id,
                v.Vin.Value,
                v.StockNumber ?? string.Empty,
                v.Make,
                v.Model,
                v.Year,
                analytics.DaysInInventory(v),
                analytics.GetAgingSeverity(v).ToString(),
                v.AskingPrice.Amount,
                v.AskingPrice.Currency,
                analytics.GetDemandScore(v),
                analytics.GetDemandLevel(v).ToString()))
            .ToList();

        var total = aging.Count;
        var paged = aging.Skip((page - 1) * limit).Take(limit).ToList();

        if (logger.IsEnabled(LogLevel.Debug))
            logger.LogDebug("Aging stock: minDays={MinDays}, total={Total}, returned={Returned}",
                q.MinAgeDays, total, paged.Count);

        return Result<PagedResult<AgingStockItemDto>>.Success(new PagedResult<AgingStockItemDto>(paged, page, limit, total));
    }
}
