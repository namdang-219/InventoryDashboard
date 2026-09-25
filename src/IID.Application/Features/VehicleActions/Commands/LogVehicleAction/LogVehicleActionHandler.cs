using IID.Application.Common.Interfaces;
using IID.Application.Logging;
using MediatR;

namespace IID.Application.VehicleActions.Commands.LogVehicleAction;

public sealed class LogVehicleActionHandler(
    IVehicleRepository vehicles,
    IVehicleActionRepository actions,
    IUnitOfWork uow,
    IVehicleHubNotifier notifier,
    IClock clock,
    ICurrentUser currentUser,
    ILogger<LogVehicleActionHandler> logger) : IRequestHandler<LogVehicleActionCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(LogVehicleActionCommand c, CancellationToken ct)
    {
        if (currentUser.Id is null)
            return Result<Guid>.Failure(ErrorKind.Unauthorized, "No authenticated user.");

        var vehicle = await vehicles.GetByIdAsync(c.VehicleId, ct);
        if (vehicle is null)
            return Result<Guid>.Failure(ErrorKind.NotFound, "Vehicle not found.");

        var loggedBy = currentUser.Email ?? currentUser.UserName ?? currentUser.Id ?? "Unknown";
        var action = VehicleAction.Log(
            c.VehicleId, c.ActionType, c.Notes, loggedBy, clock.UtcNow);

        await actions.AddAsync(action, ct);
        var saveResult = await uow.SaveChangesAsync(ct);
        if (!saveResult.IsSuccess)
            return Result<Guid>.Failure(saveResult.ErrorKind, saveResult.Message ?? "Save failed.");

        logger.VehicleActionLogged(action.Id, action.VehicleId);

        await notifier.VehicleActionLoggedAsync(action, vehicle, ct);
        await notifier.InventoryChangedAsync(ct);
        return Result<Guid>.Success(action.Id);
    }
}
