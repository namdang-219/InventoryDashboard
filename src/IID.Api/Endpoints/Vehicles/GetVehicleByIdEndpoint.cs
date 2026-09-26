using FastEndpoints;
using IID.Application.Vehicles.Queries.GetVehicleById;
using MediatR;

namespace IID.Api.Endpoints.Vehicles;

/// <summary>
/// GET /api/v1/vehicles/{id} — retrieves a single vehicle by ID.
/// </summary>
public sealed class GetVehicleByIdEndpoint(ISender sender) : EndpointWithoutRequest<object>
{
    public override void Configure()
    {
        Get("/api/v1/vehicles/{id}");
        Roles("Manager", "Sales", "Saler");
        Description(x => x.WithTags("Vehicles"));
        Summary(s =>
        {
            s.Summary = "Get vehicle by ID";
            s.Description = "Retrieves full details and specification of a specific vehicle by its unique ID.";
            s.Response(200, "Vehicle retrieved successfully.");
            s.Response(404, "Vehicle not found.");
            s.Response(401, "Unauthorized.");
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var id = Route<Guid>("id");
        var result = await sender.Send(new GetVehicleByIdQuery(id), ct);
        if (!result.IsSuccess)
        {
            AddError(result.Message ?? "Failed to find vehicle.");
            await Send.ErrorsAsync(ResultMapper.ToStatus(result.ErrorKind), ct);
            return;
        }

        await Send.OkAsync(new { data = result.Value }, ct);
    }
}
