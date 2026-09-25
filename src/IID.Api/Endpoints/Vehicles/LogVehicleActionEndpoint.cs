using FastEndpoints;
using IID.Application.VehicleActions.Commands.LogVehicleAction;
using MediatR;

namespace IID.Api.Endpoints.Vehicles;

/// <summary>
/// POST /api/v1/vehicles/{id}/actions — append an action (e.g. price change, test-drive) to a vehicle.
/// </summary>
public sealed class LogVehicleActionEndpoint(ISender sender) : Endpoint<LogVehicleActionRequest, object>
{
    public override void Configure()
    {
        Post("/api/v1/vehicles/{id}/actions");
        Roles("Manager");
        Description(x => x.WithTags("Vehicles"));
        Summary(s =>
        {
            s.Summary = "Log action on a vehicle";
            s.Description = "Appends a new business or operational action record to the vehicle audit trail.";
            s.Response(201, "Vehicle action logged successfully.");
            s.Response(400, "Validation failed.");
            s.Response(404, "Vehicle not found.");
            s.Response(401, "Unauthorized.");
            s.Response(403, "Forbidden.");
        });
    }

    public override async Task HandleAsync(LogVehicleActionRequest req, CancellationToken ct)
    {
        var cmd = new LogVehicleActionCommand(
            Route<Guid>("id"),
            Enum.Parse<Domain.VehicleActions.VehicleActionType>(req.ActionType),
            req.Notes);
        var result = await sender.Send(cmd, ct);
        if (!result.IsSuccess)
        {
            AddError(result.Message ?? "Failed to log vehicle action.");
            await Send.ErrorsAsync(ResultMapper.ToStatus(result.ErrorKind), ct);
            return;
        }
        await Send.CreatedAtAsync($"/api/v1/vehicle-actions/{result.Value}",
            new { data = new { id = result.Value } }, cancellation: ct);
    }
}

public sealed class LogVehicleActionRequest
{
    public string ActionType { get; set; } = "Other";
    public string? Notes { get; set; }
}
