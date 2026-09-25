using FastEndpoints;
using IID.Application.Dashboard.Queries.GetLowInventoryAlerts;
using MediatR;

namespace IID.Api.Endpoints.Dashboard;

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
        Summary(s =>
        {
            s.Summary = "Get low inventory alerts";
            s.Description = "Retrieves active warnings and threshold alerts for models with critically low inventory stock.";
            s.Response(200, "Low inventory alerts retrieved successfully.");
            s.Response(401, "Unauthorized.");
        });
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
