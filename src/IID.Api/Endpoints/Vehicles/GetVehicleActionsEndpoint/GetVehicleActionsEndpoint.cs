using FastEndpoints;
using IID.Api.Extensions;
using IID.Application.VehicleActions.Queries.GetVehicleActions;
using MediatR;

namespace IID.Api.Endpoints.Vehicles.GetVehicleActionsEndpoint;

/// <summary>
/// GET /api/v1/vehicles/{id}/actions — retrieves paginated actions logged on a vehicle.
/// </summary>
public sealed class GetVehicleActionsEndpoint(ISender sender) : Endpoint<GetVehicleActionsRequest, object>
{
    public override void Configure()
    {
        Get("/api/v1/vehicles/{id}/actions");
        Roles("Manager", "Viewer");
        Description(x => x.WithTags("Vehicles"));
    }

    public override async Task HandleAsync(GetVehicleActionsRequest req, CancellationToken ct)
    {
        var vehicleId = Route<Guid>("id");
        var result = await sender.Send(new GetVehicleActionsQuery(vehicleId, req.Page, req.Limit), ct);

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

public sealed class GetVehicleActionsRequest
{
    [QueryParam] public int Page { get; set; } = 1;
    [QueryParam] public int Limit { get; set; } = 50;
}
