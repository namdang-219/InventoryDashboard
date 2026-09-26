using FastEndpoints;
using IID.Application.Vehicles.Queries.ListVehicles;
using IID.Domain.Vehicles;
using MediatR;

namespace IID.Api.Endpoints.Vehicles;

/// <summary>
/// GET /api/v1/vehicles — paginated, filterable, sortable vehicle listing.
/// </summary>
public sealed class ListVehiclesEndpoint(ISender sender) : Endpoint<ListVehiclesRequest, object>
{
    public override void Configure()
    {
        Get("/api/v1/vehicles");
        Roles("Manager", "Sales", "Saler");
        Description(x => x.WithTags("Vehicles"));
        Summary(s =>
        {
            s.Summary = "List vehicles with filters";
            s.Description = "Retrieves a paginated list of inventory vehicles with optional filters for make, model, age, status, and dealership.";
            s.Response(200, "Vehicles retrieved successfully.");
            s.Response(400, "Validation failed.");
            s.Response(401, "Unauthorized.");
        });
    }

    public override async Task HandleAsync(ListVehiclesRequest req, CancellationToken ct)
    {
        var q = new ListVehiclesQuery(
            req.Make, req.Model, req.MinAgeDays, req.MaxAgeDays,
            req.Status is null ? null : Enum.Parse<VehicleStatus>(req.Status),
            req.Page, req.Limit, req.Sort, req.Order,
            req.DealershipId,
            req.Vin, req.StockNumber ?? req.Stock);
        var result = await sender.Send(q, ct);
        if (!result.IsSuccess)
        {
            AddError(result.Message ?? "Failed to load vehicles.");
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
    [QueryParam] public string? Vin { get; set; }
    [QueryParam] public string? StockNumber { get; set; }
    [QueryParam] public string? Stock { get; set; }
    [QueryParam] public int? MinAgeDays { get; set; }
    [QueryParam] public int? MaxAgeDays { get; set; }
    [QueryParam] public string? Status { get; set; }
    [QueryParam] public int Page { get; set; } = 1;
    [QueryParam] public int Limit { get; set; } = 20;
    [QueryParam] public string Sort { get; set; } = "createdAt";
    [QueryParam] public string Order { get; set; } = "desc";
}
