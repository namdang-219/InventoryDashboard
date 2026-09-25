using FastEndpoints;
using IID.Api.Extensions;
using IID.Application.Features.Dealerships.Commands.UpdateDealership;
using MediatR;

namespace IID.Api.Endpoints.Dealerships;

/// <summary>
/// PUT /api/v1/dealerships/{id} — updates dealership details.
/// </summary>
public sealed class UpdateDealershipEndpoint(ISender sender) : Endpoint<UpdateDealershipRequest, object>
{
    public override void Configure()
    {
        Put("/api/v1/dealerships/{id}");
        Roles("Manager");
        Description(x => x.WithTags("Dealerships"));
        Summary(s =>
        {
            s.Description = "Update an existing dealership location.";
            s.Response(200, "Dealership updated successfully.");
            s.Response(404, "Dealership not found.");
            s.Response(400, "Validation failed or duplicate code.");
        });
    }

    public override async Task HandleAsync(UpdateDealershipRequest req, CancellationToken ct)
    {
        var id = Route<Guid>("id");
        var cmd = new UpdateDealershipCommand(
            id,
            req.Name,
            req.Code,
            req.City,
            req.State,
            req.Phone);

        var result = await sender.Send(cmd, ct);
        if (!result.IsSuccess)
        {
            AddError(result.Message ?? "Failed to update dealership.");
            await Send.ErrorsAsync(ResultMapper.ToStatus(result.ErrorKind), ct);
            return;
        }

        await Send.OkAsync(new { data = new { id = result.Value } }, ct);
    }
}

public sealed record UpdateDealershipRequest(
    string Name,
    string Code,
    string City,
    string State,
    string Phone);
