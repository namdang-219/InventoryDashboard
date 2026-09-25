using IID.Application.Common.Interfaces;
using MediatR;

namespace IID.Application.Features.Activities.Commands.MarkActivityRead;

public sealed class MarkActivityReadHandler(
    IUserActivityReadRepository userActivityReadRepository,
    ICurrentUser currentUser) : IRequestHandler<MarkActivityReadCommand, Result>
{
    public async Task<Result> Handle(MarkActivityReadCommand cmd, CancellationToken ct)
    {
        var userId = currentUser.Id;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Result.Failure(ErrorKind.Unauthorized, "User is not authenticated.");
        }

        if (cmd.All == true)
        {
            await userActivityReadRepository.MarkAllAsReadAsync(userId, ct);
        }
        else if (cmd.ActivityIds is { Count: > 0 })
        {
            await userActivityReadRepository.MarkAsReadAsync(userId, cmd.ActivityIds, ct);
        }

        return Result.Success();
    }
}
