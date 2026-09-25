using FastEndpoints;
using IID.Application.VehicleActions.Queries.GetVehicleActions;
using MediatR;

namespace IID.Api.Endpoints.VehicleActions;

public sealed class ListVehicleActionsRequest
{
    [QueryParam] public Guid? VehicleId { get; set; }
    [QueryParam] public string? ActionType { get; set; }
    [QueryParam] public string? Search { get; set; }
    [QueryParam] public int Page { get; set; } = 1;
    [QueryParam] public int Limit { get; set; } = 20;
}

public sealed class ListVehicleActionsEndpoint(ISender sender) : Endpoint<ListVehicleActionsRequest, object>
{
    public override void Configure()
    {
        Get("/api/v1/vehicle-actions");
        Roles("Manager", "Saler");
        Description(x => x.WithTags("VehicleActions"));
        Summary(s =>
        {
            s.Summary = "List all vehicle actions";
            s.Description = "Retrieves paginated audit history of actions with optional filtering by vehicle, action type, or search term.";
            s.Response(200, "Vehicle actions retrieved successfully.");
            s.Response(401, "Unauthorized.");
        });
    }

    public override async Task HandleAsync(ListVehicleActionsRequest req, CancellationToken ct)
    {
        var result = await sender.Send(new GetVehicleActionsQuery(
            req.VehicleId,
            req.Page,
            req.Limit,
            req.ActionType,
            req.Search), ct);

        if (!result.IsSuccess)
        {
            AddError(result.Message ?? "Failed to retrieve vehicle actions.");
            await Send.ErrorsAsync(ResultMapper.ToStatus(result.ErrorKind), ct);
            return;
        }

        var paged = result.Value!;
        await Send.OkAsync(new
        {
            data = paged.Items,
            meta = new { paged.Page, paged.Limit, paged.Total }
        }, ct);
    }
}
