using FastEndpoints;
using IID.Api.Extensions;
using IID.Application.Vehicles.Queries.ListVehicles;
using IID.Domain.Vehicles;
using MediatR;

namespace IID.Api.Endpoints.ListVehiclesEndpoint;

/// <summary>
/// GET /api/v1/vehicles — paginated, filterable, sortable vehicle listing.
/// Folder layout mirrors Sportcast's <c>Endpoints/RequestBetPriceByUI</c>.
/// </summary>
public sealed class ListVehiclesEndpoint(ISender sender) : Endpoint<ListVehiclesRequest, object>
{
    public override void Configure()
    {
        Get("/api/v1/vehicles");
        Roles("Manager", "Viewer");
        Description(x => x.WithTags("Vehicles"));
    }

    public override async Task HandleAsync(ListVehiclesRequest req, CancellationToken ct)
    {
        var q = new ListVehiclesQuery(
            req.Make, req.Model, req.MinAgeDays, req.MaxAgeDays,
            req.Status is null ? null : Enum.Parse<VehicleStatus>(req.Status),
            req.Page, req.Limit, req.Sort, req.Order,
            req.DealershipId);
        var result = await sender.Send(q, ct);
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

public sealed class ListVehiclesRequest
{
    [QueryParam] public Guid? DealershipId { get; set; }
    [QueryParam] public string? Make { get; set; }
    [QueryParam] public string? Model { get; set; }
    [QueryParam] public int? MinAgeDays { get; set; }
    [QueryParam] public int? MaxAgeDays { get; set; }
    [QueryParam] public string? Status { get; set; }
    [QueryParam] public int Page { get; set; } = 1;
    [QueryParam] public int Limit { get; set; } = 20;
    [QueryParam] public string Sort { get; set; } = "createdAt";
    [QueryParam] public string Order { get; set; } = "desc";
}
