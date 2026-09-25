using FastEndpoints;
using IID.Application.VehicleActions.Commands.LogVehicleAction;
using MediatR;

namespace IID.Api.Endpoints.VehicleActions;

public sealed class CreateVehicleActionRequest
{
    public Guid VehicleId { get; set; }
    public string ActionType { get; set; } = "Other";
    public string? Notes { get; set; }
}

public sealed class CreateVehicleActionEndpoint(ISender sender) : Endpoint<CreateVehicleActionRequest, object>
{
    public override void Configure()
    {
        Post("/api/v1/vehicle-actions");
        Roles("Manager", "Saler");
        Description(x => x.WithTags("VehicleActions"));
        Summary(s =>
        {
            s.Summary = "Log a new vehicle action";
            s.Description = "Appends a new business or operational action record to the vehicle audit trail.";
            s.Response(201, "Vehicle action created successfully.");
            s.Response(400, "Validation failed.");
            s.Response(404, "Vehicle not found.");
            s.Response(401, "Unauthorized.");
        });
    }

    public override async Task HandleAsync(CreateVehicleActionRequest req, CancellationToken ct)
    {
        if (!Enum.TryParse<Domain.VehicleActions.VehicleActionType>(req.ActionType, true, out var actionType))
            actionType = Domain.VehicleActions.VehicleActionType.Other;

        var cmd = new LogVehicleActionCommand(req.VehicleId, actionType, req.Notes);
        var result = await sender.Send(cmd, ct);

        if (!result.IsSuccess)
        {
            AddError(result.Message ?? "Failed to log vehicle action.");
            await Send.ErrorsAsync(ResultMapper.ToStatus(result.ErrorKind), ct);
            return;
        }

        HttpContext.Response.StatusCode = 201;
        await Send.OkAsync(new { data = new { id = result.Value } }, ct);
    }
}
