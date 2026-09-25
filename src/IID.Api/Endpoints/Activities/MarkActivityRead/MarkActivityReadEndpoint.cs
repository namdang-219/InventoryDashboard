using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using IID.Application.Common.Interfaces;

namespace IID.Api.Endpoints.Activities.MarkActivityRead;

public sealed record MarkActivityReadRequest(List<string>? ActivityIds, bool? All);

public sealed class MarkActivityReadEndpoint(
    IUserActivityReadRepository readRepo,
    ICurrentUser currentUser) : Endpoint<MarkActivityReadRequest, object>
{
    public override void Configure()
    {
        Post("/api/v1/activities/read");
        Roles("Manager", "Viewer");
        Description(x => x.WithTags("Activities"));
    }

    public override async Task HandleAsync(MarkActivityReadRequest req, CancellationToken ct)
    {
        var userId = currentUser.Id;
        if (string.IsNullOrWhiteSpace(userId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        if (req.All == true)
        {
            await readRepo.MarkAllAsReadAsync(userId, ct);
        }
        else if (req.ActivityIds is { Count: > 0 })
        {
            await readRepo.MarkAsReadAsync(userId, req.ActivityIds, ct);
        }

        await Send.OkAsync(new { success = true }, ct);
    }
}
