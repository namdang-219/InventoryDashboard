using FastEndpoints;
using IID.Api.Extensions;
using IID.Application.Common.Models;
using IID.Application.Dashboard.Queries.Dtos;
using IID.Application.Dashboard.Queries.GetDashboardBundle;
using MediatR;

namespace IID.Api.Endpoints.Dashboard.GetDashboardBundle;

/// <summary>
/// GET /api/v1/dashboard — full dashboard bundle (summary, quick-stats, charts, actions, inventory).
/// </summary>
public sealed class GetDashboardBundleEndpoint(ISender sender)
    : Endpoint<GetDashboardBundleRequest, object>
{
    public override void Configure()
    {
        Get("/api/v1/dashboard");
        Roles("Manager", "Viewer");
        Description(x => x.WithTags("Dashboard"));
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
