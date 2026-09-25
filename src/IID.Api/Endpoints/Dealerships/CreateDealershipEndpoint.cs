using FastEndpoints;
using IID.Api.Extensions;
using IID.Application.Features.Dealerships.Commands.CreateDealership;
using MediatR;

namespace IID.Api.Endpoints.Dealerships;

/// <summary>
/// POST /api/v1/dealerships — registers a new dealership in the system.
/// </summary>
public sealed class CreateDealershipEndpoint(ISender sender) : Endpoint<CreateDealershipRequest, object>
{
    public override void Configure()
    {
        Post("/api/v1/dealerships");
        Roles("Manager");
        Description(x => x.WithTags("Dealerships"));
        Summary(s =>
        {
            s.Description = "Create a new dealership location.";
            s.Response(201, "Dealership created successfully.");
            s.Response(400, "Validation failed or duplicate code.");
        });
    }

    public override async Task HandleAsync(CreateDealershipRequest req, CancellationToken ct)
    {
        var cmd = new CreateDealershipCommand(
            req.Name,
            req.Code,
            req.City,
            req.State,
            req.Phone);

        var result = await sender.Send(cmd, ct);
        if (!result.IsSuccess)
        {
            AddError(result.Message ?? "Failed to create dealership.");
            await Send.ErrorsAsync(ResultMapper.ToStatus(result.ErrorKind), ct);
            return;
        }

        await Send.CreatedAtAsync(
            endpointName: "ListDealerships",
            responseBody: new { id = result.Value },
            cancellation: ct);
    }
}

public sealed record CreateDealershipRequest(
    string Name,
    string Code,
    string City,
    string State,
    string Phone);
