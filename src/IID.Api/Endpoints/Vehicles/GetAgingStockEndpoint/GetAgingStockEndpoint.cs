using FastEndpoints;
using IID.Api.Extensions;
using IID.Application.Vehicles.Queries.GetAgingStock;
using MediatR;

namespace IID.Api.Endpoints.GetAgingStockEndpoint;

/// <summary>
/// GET /api/v1/vehicles/aging-stock — lists vehicles that have been on the lot longer than a threshold.
/// Folder layout mirrors Sportcast's <c>Endpoints/RequestBetPriceByUI</c>.
/// </summary>
public sealed class GetAgingStockEndpoint(ISender sender) : Endpoint<GetAgingStockRequest, object>
{
    public override void Configure()
    {
        Get("/api/v1/vehicles/aging-stock");
        Roles("Manager", "Viewer");
        Description(x => x.WithTags("Vehicles"));
    }

    public override async Task HandleAsync(GetAgingStockRequest req, CancellationToken ct)
    {
        var result = await sender.Send(new GetAgingStockQuery(req.Page, req.Limit), ct);
        if (!result.IsSuccess)
        {
            AddError(result.Message ?? "Failed");
            await Send.ErrorsAsync(ResultMapper.ToStatus(result.ErrorKind), ct);
            return;
        }
        var paged = result.Value!;
        await Send.OkAsync(new { data = paged.Items, meta = new { paged.Page, paged.Limit, paged.Total } }, ct);
    }
}

public sealed class GetAgingStockRequest
{
    [QueryParam] public int Page { get; set; } = 1;
    [QueryParam] public int Limit { get; set; } = 20;
}
