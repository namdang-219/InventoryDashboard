using IID.Application.Common.Interfaces;
using IID.Application.Logging;
using MediatR;

namespace IID.Application.Features.Activities.Commands.MarkActivityRead;

public sealed class MarkActivityReadHandler(
    IUserActivityReadRepository userActivityReadRepository,
    ICurrentUser currentUser,
    ILogger<MarkActivityReadHandler> logger) : IRequestHandler<MarkActivityReadCommand, Result>
{
    public async Task<Result> Handle(MarkActivityReadCommand cmd, CancellationToken ct)
    {
        var userId = currentUser.Id;
        if (string.IsNullOrWhiteSpace(userId))
        {
            logger.MarkActivityReadRejected("User is not authenticated.");
            return Result.Failure(ErrorKind.Unauthorized, "User is not authenticated.");
        }

        var mode = cmd.All == true
            ? "all"
            : (cmd.ActivityIds is { Count: > 0 } ? $"ids:{cmd.ActivityIds.Count}" : "none");
        var count = cmd.ActivityIds?.Count ?? 0;

        if (cmd.All == true)
        {
            await userActivityReadRepository.MarkAllAsReadAsync(userId, ct);
        }
        else if (cmd.ActivityIds is { Count: > 0 })
        {
            await userActivityReadRepository.MarkAsReadAsync(userId, cmd.ActivityIds, ct);
        }

        logger.ActivitiesMarkedRead(userId, mode, count);
        return Result.Success();
    }
}
