using FastEndpoints;
using IID.Application.Features.Activities.Queries.GetActivities;
using MediatR;

namespace IID.Api.Endpoints.Activities;

public sealed record GetActivitiesRequest
{
    [QueryParam] public int Limit { get; set; } = 10;
    [QueryParam] public string? Cursor { get; set; }
}

public sealed class GetActivitiesEndpoint(ISender sender) : Endpoint<GetActivitiesRequest, object>
{
    public override void Configure()
    {
        Get("/api/v1/activities");
        Roles("Manager", "Viewer");
        Description(x => x.WithTags("Activities"));
        Summary(s =>
        {
            s.Summary = "Get user activities";
            s.Description = "Retrieves recent activities and vehicle action logs with cursor-based pagination and read status.";
            s.Response(200, "Activities retrieved successfully.");
            s.Response(400, "Validation failed.");
            s.Response(401, "Unauthorized.");
        });
    }

    public override async Task HandleAsync(GetActivitiesRequest req, CancellationToken ct)
    {
        var result = await sender.Send(new GetActivitiesQuery(req.Limit, req.Cursor), ct);
        if (!result.IsSuccess)
        {
            AddError(result.Message ?? "Failed to retrieve activities");
            await Send.ErrorsAsync(ResultMapper.ToStatus(result.ErrorKind), ct);
            return;
        }

        await Send.OkAsync(result.Value!, ct);
    }
}
