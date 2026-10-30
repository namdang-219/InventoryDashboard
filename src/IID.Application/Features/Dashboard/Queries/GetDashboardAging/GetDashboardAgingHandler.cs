using IID.Application.Common.Interfaces;
using IID.Application.Common.Models;
using IID.Application.Dashboard.Queries.Dtos;
using MediatR;

namespace IID.Application.Dashboard.Queries.GetDashboardAging;

public sealed class GetDashboardAgingHandler(
    IVehicleRepository vehicles,
    IClock clock) : IRequestHandler<GetDashboardAgingQuery, Result<PagedResult<AgingStockItemDto>>>
{
    public async Task<Result<PagedResult<AgingStockItemDto>>> Handle(GetDashboardAgingQuery q, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var page = Math.Max(1, q.Page);
        var limit = Math.Clamp(q.Limit, 1, 100);

        var (items, total) = await vehicles.ListAgingAsync(
            q.MinAgeDays,
            page,
            limit,
            q.DealershipId,
            now,
            ct);

        var analytics = new VehicleAnalyticsService(now);
        var paged = items
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

        return Result<PagedResult<AgingStockItemDto>>.Success(
            new PagedResult<AgingStockItemDto>(paged, page, limit, total));
    }
}
