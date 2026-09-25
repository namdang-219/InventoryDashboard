using FastEndpoints;
using IID.Application.Dashboard.Queries.GetDashboardAging;
using MediatR;

namespace IID.Api.Endpoints.Dashboard;

/// <summary>
/// GET /api/v1/dashboard/aging — aging vehicles (>60 days on lot).
/// </summary>
public sealed class GetDashboardAgingEndpoint(ISender sender)
    : Endpoint<GetDashboardAgingRequest, object>
{
    public override void Configure()
    {
        Get("/api/v1/dashboard/aging");
        Roles("Manager", "Saler");
        Description(x => x.WithTags("Dashboard"));
        Summary(s =>
        {
            s.Summary = "Get aging vehicles for dashboard";
            s.Description = "Retrieves a paginated list of aging vehicles exceeding the specified age threshold.";
            s.Response(200, "Aging vehicles retrieved successfully.");
            s.Response(400, "Validation failed.");
            s.Response(401, "Unauthorized.");
        });
    }

    public override async Task HandleAsync(GetDashboardAgingRequest req, CancellationToken ct)
    {
        var result = await sender.Send(
            new GetDashboardAgingQuery(req.MinAgeDays, req.Page, req.Limit, req.DealershipId), ct);

        if (!result.IsSuccess)
        {
            AddError(result.Message ?? "Failed to load aging stock.");
            await Send.ErrorsAsync(ResultMapper.ToStatus(result.ErrorKind), ct);
            return;
        }

        var paged = result.Value!;
        await Send.OkAsync(new
        {
            data = paged.Items,
            meta = new { paged.Page, paged.Limit, paged.Total }
        }, ct);
    }
}

public sealed class GetDashboardAgingRequest
{
    [QueryParam] public Guid? DealershipId { get; set; }
    [QueryParam] public int MinAgeDays { get; set; } = 60;
    [QueryParam] public int Page { get; set; } = 1;
    [QueryParam] public int Limit { get; set; } = 20;
}
