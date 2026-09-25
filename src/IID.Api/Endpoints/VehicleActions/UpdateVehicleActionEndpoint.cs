using FastEndpoints;
using IID.Application.VehicleActions.Commands.UpdateVehicleAction;
using MediatR;

namespace IID.Api.Endpoints.VehicleActions;

public sealed class UpdateVehicleActionRequest
{
    public string ActionType { get; set; } = "Other";
    public string? Notes { get; set; }
}

public sealed class UpdateVehicleActionEndpoint(ISender sender) : Endpoint<UpdateVehicleActionRequest, object>
{
    public override void Configure()
    {
        Put("/api/v1/vehicle-actions/{id}");
        Roles("Manager", "Saler");
        Description(x => x.WithTags("VehicleActions"));
        Summary(s =>
        {
            s.Summary = "Update vehicle action";
            s.Description = "Updates the action type and notes of an existing vehicle action.";
            s.Response(200, "Vehicle action updated successfully.");
            s.Response(400, "Validation failed.");
            s.Response(404, "Vehicle action not found.");
            s.Response(401, "Unauthorized.");
        });
    }

    public override async Task HandleAsync(UpdateVehicleActionRequest req, CancellationToken ct)
    {
        var id = Route<Guid>("id");
        if (!Enum.TryParse<Domain.VehicleActions.VehicleActionType>(req.ActionType, true, out var actionType))
            actionType = Domain.VehicleActions.VehicleActionType.Other;

        var cmd = new UpdateVehicleActionCommand(id, actionType, req.Notes);
        var result = await sender.Send(cmd, ct);

        if (!result.IsSuccess)
        {
            AddError(result.Message ?? "Failed to update vehicle action.");
            await Send.ErrorsAsync(ResultMapper.ToStatus(result.ErrorKind), ct);
            return;
        }

        await Send.OkAsync(new { data = new { id } }, ct);
    }
}
