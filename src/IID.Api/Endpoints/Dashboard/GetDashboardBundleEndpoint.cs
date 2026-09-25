using FastEndpoints;
using IID.Application.Dashboard.Queries.GetDashboardBundle;
using MediatR;

namespace IID.Api.Endpoints.Dashboard;

/// <summary>
/// GET /api/v1/dashboard — full dashboard bundle (summary, quick-stats, charts, actions, inventory).
/// </summary>
public sealed class GetDashboardBundleEndpoint(ISender sender)
    : Endpoint<GetDashboardBundleRequest, object>
{
    public override void Configure()
    {
        Get("/api/v1/dashboard");
        Roles("Manager", "Saler");
        Description(x => x.WithTags("Dashboard"));
        Summary(s =>
        {
            s.Summary = "Get complete dashboard bundle";
            s.Description = "Retrieves aggregated dashboard data including summary metrics, aging breakdown, recent activities, and inventory status.";
            s.Response(200, "Dashboard bundle retrieved successfully.");
            s.Response(400, "Validation failed.");
            s.Response(401, "Unauthorized.");
        });
    }

    public override async Task HandleAsync(GetDashboardBundleRequest req, CancellationToken ct)
    {
        var result = await sender.Send(new GetDashboardBundleQuery(req.Page, req.PageSize, req.DealershipId), ct);

        if (!result.IsSuccess)
        {
            AddError(result.Message ?? "Failed to load dashboard.");
            await Send.ErrorsAsync(ResultMapper.ToStatus(result.ErrorKind), ct);
            return;
        }

        var bundle = result.Value!;
        await Send.OkAsync(new
        {
            data = bundle,
            meta = new { bundle.Summary.TotalInventory }
        }, ct);
    }
}

public sealed class GetDashboardBundleRequest
{
    [QueryParam] public Guid? DealershipId { get; set; }
    [QueryParam] public int Page { get; set; } = 1;
    [QueryParam] public int PageSize { get; set; } = 20;
}
