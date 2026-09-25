using FastEndpoints;
using IID.Application.Vehicles.Commands.TransferDealership;
using MediatR;

namespace IID.Api.Endpoints.Vehicles;

/// <summary>
/// POST /api/v1/vehicles/{id}/transfer — transfers vehicle to another dealership showroom.
/// </summary>
public sealed class TransferDealershipEndpoint(ISender sender) : Endpoint<TransferDealershipRequest, object>
{
    public override void Configure()
    {
        Post("/api/v1/vehicles/{id}/transfer");
        Roles("Manager");
        Description(x => x.WithTags("Vehicles"));
        Summary(s =>
        {
            s.Description = "Transfer a vehicle to another dealership showroom.";
            s.Response(200, "Vehicle transferred successfully.");
            s.Response(404, "Vehicle or target dealership not found.");
            s.Response(409, "Vehicle already at target dealership or already sold.");
            s.Response(422, "Validation failed.");
        });
    }

    public override async Task HandleAsync(TransferDealershipRequest req, CancellationToken ct)
    {
        var cmd = new TransferDealershipCommand(
            Route<Guid>("id"),
            req.TargetDealershipId,
            req.Notes);

        var result = await sender.Send(cmd, ct);
        if (!result.IsSuccess)
        {
            AddError(result.Message ?? "Failed to transfer vehicle.");
            await Send.ErrorsAsync(ResultMapper.ToStatus(result.ErrorKind), ct);
            return;
        }

        await Send.OkAsync(new { data = new { id = result.Value } }, ct);
    }
}

public sealed class TransferDealershipRequest
{
    public Guid TargetDealershipId { get; set; }
    public string? Notes { get; set; }
}
