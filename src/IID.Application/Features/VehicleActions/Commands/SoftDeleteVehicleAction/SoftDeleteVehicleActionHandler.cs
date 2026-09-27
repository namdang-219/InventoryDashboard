using IID.Application.Common.Interfaces;
using IID.Application.Common.Models;
using IID.Application.Logging;
using MediatR;

namespace IID.Application.VehicleActions.Commands.SoftDeleteVehicleAction;

public sealed class SoftDeleteVehicleActionHandler(
    IVehicleActionRepository actions,
    IUnitOfWork uow,
    IClock clock,
    ICurrentUser currentUser,
    ILogger<SoftDeleteVehicleActionHandler> logger) : IRequestHandler<SoftDeleteVehicleActionCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(SoftDeleteVehicleActionCommand c, CancellationToken ct)
    {
        if (currentUser.Id is null)
        {
            logger.SoftDeleteVehicleActionRejected(c.Id, "No authenticated user.");
            return Result<Unit>.Failure(ErrorKind.Unauthorized, "No authenticated user.");
        }

        var action = await actions.GetByIdAsync(c.Id, ct);
        if (action is null)
        {
            logger.SoftDeleteVehicleActionRejected(c.Id, "Vehicle action not found.");
            return Result<Unit>.Failure(ErrorKind.NotFound, "Vehicle action not found.");
        }

        var deletedBy = currentUser.Email ?? currentUser.UserName ?? currentUser.Id ?? "Unknown";
        action.SoftDelete(clock.UtcNow, deletedBy);

        actions.Update(action);
        var saveResult = await uow.SaveChangesAsync(ct);
        if (!saveResult.IsSuccess)
        {
            logger.SoftDeleteVehicleActionFailed(c.Id, saveResult.Message ?? "Save failed.", null);
            return Result<Unit>.Failure(saveResult.ErrorKind, saveResult.Message ?? "Save failed.");
        }

        logger.VehicleActionSoftDeleted(action.Id, currentUser.Id);
        return Result<Unit>.Success(Unit.Value);
    }
}
