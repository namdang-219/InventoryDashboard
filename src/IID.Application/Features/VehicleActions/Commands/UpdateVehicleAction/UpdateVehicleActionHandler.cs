using IID.Application.Common.Interfaces;
using IID.Application.Common.Models;
using MediatR;

namespace IID.Application.VehicleActions.Commands.UpdateVehicleAction;

public sealed class UpdateVehicleActionHandler(
    IVehicleActionRepository actions,
    IUnitOfWork uow,
    IClock clock,
    ICurrentUser currentUser) : IRequestHandler<UpdateVehicleActionCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(UpdateVehicleActionCommand c, CancellationToken ct)
    {
        if (currentUser.Id is null)
            return Result<Unit>.Failure(ErrorKind.Unauthorized, "No authenticated user.");

        var action = await actions.GetByIdAsync(c.Id, ct);
        if (action is null)
            return Result<Unit>.Failure(ErrorKind.NotFound, "Vehicle action not found.");

        var updatedBy = currentUser.Email ?? currentUser.UserName ?? currentUser.Id ?? "Unknown";
        action.Update(c.ActionType, c.Notes, clock.UtcNow, updatedBy);

        actions.Update(action);
        var saveResult = await uow.SaveChangesAsync(ct);
        if (!saveResult.IsSuccess)
            return Result<Unit>.Failure(saveResult.ErrorKind, saveResult.Message ?? "Save failed.");

        return Result<Unit>.Success(Unit.Value);
    }
}
