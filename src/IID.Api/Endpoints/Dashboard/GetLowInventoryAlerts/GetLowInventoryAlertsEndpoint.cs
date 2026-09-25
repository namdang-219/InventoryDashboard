using FastEndpoints;
using IID.Api.Extensions;
using IID.Application.Dashboard.Queries.Dtos;
using IID.Application.Dashboard.Queries.GetLowInventoryAlerts;
using MediatR;

namespace IID.Api.Endpoints.Dashboard.GetLowInventoryAlerts;

/// <summary>
/// GET /api/v1/dashboard/alerts/low-inventory — alerts for low-availability make/model combos.
/// </summary>
public sealed class GetLowInventoryAlertsEndpoint(ISender sender)
    : EndpointWithoutRequest<object>
{
    public override void Configure()
    {
        Get("/api/v1/dashboard/alerts/low-inventory");
        Roles("Manager", "Viewer");
        Description(x => x.WithTags("Dashboard", "Alerts"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await sender.Send(new GetLowInventoryAlertsQuery(), ct);

        if (!result.IsSuccess)
        {
            AddError(result.Message ?? "Failed to load alerts.");
            await Send.ErrorsAsync(ResultMapper.ToStatus(result.ErrorKind), ct);
            return;
        }

        await Send.OkAsync(new { data = result.Value! }, ct);
    }
}
