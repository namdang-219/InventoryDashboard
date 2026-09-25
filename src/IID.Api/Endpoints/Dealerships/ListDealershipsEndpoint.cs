using FastEndpoints;
using IID.Application.Features.Dealerships.Queries.GetDealerships;
using MediatR;

namespace IID.Api.Endpoints.Dealerships;

/// <summary>
/// GET /api/v1/dealerships — list all dealerships.
/// </summary>
public sealed class ListDealershipsEndpoint(ISender sender) : EndpointWithoutRequest<object>
{
    public override void Configure()
    {
        Get("/api/v1/dealerships");
        Roles("Manager", "Saler");
        Description(x => x.WithTags("Dealerships"));
        Summary(s =>
        {
            s.Summary = "List all dealerships";
            s.Description = "Retrieves all active dealership locations in the system.";
            s.Response(200, "Dealerships loaded successfully.");
            s.Response(401, "Unauthorized.");
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await sender.Send(new GetDealershipsQuery(), ct);
        if (!result.IsSuccess)
        {
            AddError(result.Message ?? "Failed to load dealerships.");
            await Send.ErrorsAsync(ResultMapper.ToStatus(result.ErrorKind), ct);
            return;
        }

        await Send.OkAsync(new { data = result.Value }, ct);
    }
}
