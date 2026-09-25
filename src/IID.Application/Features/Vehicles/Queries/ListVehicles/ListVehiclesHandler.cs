using IID.Application.Common.Interfaces;
using IID.Application.Common.Models;
using IID.Application.Logging;
using IID.Application.Vehicles.Queries.Dtos;
using MediatR;

namespace IID.Application.Vehicles.Queries.ListVehicles;

public sealed class ListVehiclesHandler(
    IVehicleRepository vehicles,
    IClock clock,
    ILogger<ListVehiclesHandler> logger) : IRequestHandler<ListVehiclesQuery, Result<PagedResult<VehicleResponse>>>
{
    public async Task<Result<PagedResult<VehicleResponse>>> Handle(ListVehiclesQuery q, CancellationToken ct)
    {
        var limit = Math.Clamp(q.Limit, 1, 100);
        var page = Math.Max(1, q.Page);

        var filter = new VehicleListFilter(
            q.Make, q.Model, q.MinAgeDays, q.MaxAgeDays, q.Status,
            page, limit, q.Sort, q.Order, q.DealershipId,
            q.Vin, q.StockNumber);
        var (items, total) = await vehicles.ListAsync(filter, ct);

        var analytics = new VehicleAnalyticsService(clock.UtcNow);
        var responses = items.Select(v => VehicleResponse.From(v, analytics)).ToList();

        if (logger.IsEnabled(LogLevel.Debug))
            logger.VehiclesListed(total, page, limit);
        return Result<PagedResult<VehicleResponse>>.Success(new PagedResult<VehicleResponse>(responses, page, limit, total));
    }
}
