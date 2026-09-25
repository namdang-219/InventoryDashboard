using IID.Application.Common.Interfaces;
using IID.Application.Common.Models;
using IID.Application.Logging;
using IID.Application.Vehicles.Queries.Dtos;
using MediatR;

namespace IID.Application.Vehicles.Queries.GetAgingStock;

public sealed class GetAgingStockHandler(
    IVehicleRepository vehicles,
    IClock clock,
    ILogger<GetAgingStockHandler> logger) : IRequestHandler<GetAgingStockQuery, Result<PagedResult<VehicleResponse>>>
{
    public async Task<Result<PagedResult<VehicleResponse>>> Handle(GetAgingStockQuery q, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var limit = Math.Clamp(q.Limit, 1, 100);
        var page = Math.Max(1, q.Page);

        var (items, total) = await vehicles.ListAsync(
            null, null, 91, null, null, page, limit, "dateAdded", "asc", ct);

        var analytics = new VehicleAnalyticsService(now);
        var filtered = items
            .Where(analytics.IsAging)
            .Select(v => VehicleResponse.From(v, analytics))
            .ToList();

        if (logger.IsEnabled(LogLevel.Debug))
            logger.AgingStockFetched(total, page, limit);
        return Result<PagedResult<VehicleResponse>>.Success(new PagedResult<VehicleResponse>(filtered, page, limit, total));
    }
}
