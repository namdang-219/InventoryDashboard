using FastEndpoints;
using IID.Application.VehicleActions.Commands.SoftDeleteVehicleAction;
using MediatR;

namespace IID.Api.Endpoints.VehicleActions;

public sealed class SoftDeleteVehicleActionEndpoint(ISender sender) : EndpointWithoutRequest<object>
{
    public override void Configure()
    {
        Delete("/api/v1/vehicle-actions/{id}");
        Roles("Manager", "Sales", "Saler");
        Description(x => x.WithTags("VehicleActions"));
        Summary(s =>
        {
            s.Summary = "Soft delete vehicle action";
            s.Description = "Marks a vehicle action as deleted without physically removing it from the database.";
            s.Response(200, "Vehicle action deleted successfully.");
            s.Response(404, "Vehicle action not found.");
            s.Response(401, "Unauthorized.");
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var id = Route<Guid>("id");
        var cmd = new SoftDeleteVehicleActionCommand(id);
        var result = await sender.Send(cmd, ct);

        if (!result.IsSuccess)
        {
            AddError(result.Message ?? "Failed to delete vehicle action.");
            await Send.ErrorsAsync(ResultMapper.ToStatus(result.ErrorKind), ct);
            return;
        }

        await Send.OkAsync(new { data = new { id } }, ct);
    }
}
