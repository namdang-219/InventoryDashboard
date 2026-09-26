using FastEndpoints;
using IID.Application.Features.Activities.Commands.MarkActivityRead;
using MediatR;

namespace IID.Api.Endpoints.Activities;

public sealed record MarkActivityReadRequest(List<string>? ActivityIds, bool? All);

public sealed class MarkActivityReadEndpoint(ISender sender) : Endpoint<MarkActivityReadRequest, object>
{
    public override void Configure()
    {
        Post("/api/v1/activities/read");
        Roles("Manager", "Sales", "Saler");
        Description(x => x.WithTags("Activities"));
        Summary(s =>
        {
            s.Summary = "Mark activities as read";
            s.Description = "Marks specific activity IDs or all activities as read for the authenticated user.";
            s.Response(200, "Activities marked as read successfully.");
            s.Response(400, "Validation failed.");
            s.Response(401, "Unauthorized.");
        });
    }

    public override async Task HandleAsync(MarkActivityReadRequest req, CancellationToken ct)
    {
        var result = await sender.Send(new MarkActivityReadCommand(req.ActivityIds, req.All), ct);
        if (!result.IsSuccess)
        {
            AddError(result.Message ?? "Failed to mark activities as read.");
            await Send.ErrorsAsync(ResultMapper.ToStatus(result.ErrorKind), ct);
            return;
        }

        await Send.OkAsync(new { success = true }, ct);
    }
}

