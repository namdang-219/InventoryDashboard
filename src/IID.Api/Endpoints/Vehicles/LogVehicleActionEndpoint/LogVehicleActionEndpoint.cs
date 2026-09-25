using FastEndpoints;
using IID.Api.Extensions;
using IID.Application.VehicleActions.Commands.LogVehicleAction;
using MediatR;

namespace IID.Api.Endpoints.LogVehicleActionEndpoint;

/// <summary>
/// POST /api/v1/vehicles/{id}/actions — append an action (e.g. price change, test-drive) to a vehicle.
/// Folder layout mirrors Sportcast's <c>Endpoints/RequestBetPriceByUI</c>.
/// </summary>
public sealed class LogVehicleActionEndpoint(ISender sender) : Endpoint<LogVehicleActionRequest, object>
{
    public override void Configure()
    {
        Post("/api/v1/vehicles/{id}/actions");
        Roles("Manager");
        Description(x => x.WithTags("Vehicles"));
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
            AddError(result.Message ?? "Failed");
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
