using IID.Application.Common.Interfaces;
using IID.Application.Logging;
using MediatR;

namespace IID.Application.VehicleActions.Commands.LogVehicleAction;

/// <summary>
/// Persists a <see cref="Domain.VehicleActions.VehicleAction"/> and enriches
/// the raised <c>VehicleActionLogged</c> domain event with the originating
/// <see cref="Vehicle"/> so <c>VehicleActionRealtimeEventHandler</c> can
/// broadcast without a DB roundtrip. No direct reference to
/// <see cref="IVehicleHubNotifier"/> from this handler.
/// </summary>
public sealed class LogVehicleActionHandler(
    IVehicleRepository vehicles,
    IVehicleActionRepository actions,
    IUnitOfWork uow,
    IClock clock,
    ICurrentUser currentUser,
    ILogger<LogVehicleActionHandler> logger) : IRequestHandler<LogVehicleActionCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(LogVehicleActionCommand c, CancellationToken ct)
    {
        if (currentUser.Id is null)
        {
            logger.LogVehicleActionRejected(c.VehicleId, "No authenticated user.");
            return Result<Guid>.Failure(ErrorKind.Unauthorized, "No authenticated user.");
        }

        var vehicle = await vehicles.GetByIdAsync(c.VehicleId, ct);
        if (vehicle is null)
        {
            logger.LogVehicleActionRejected(c.VehicleId, "Vehicle not found.");
            return Result<Guid>.Failure(ErrorKind.NotFound, "Vehicle not found.");
        }

        var loggedBy = currentUser.Email ?? currentUser.UserName ?? currentUser.Id ?? "Unknown";
        var action = VehicleAction.Log(
            c.VehicleId, c.ActionType, c.Notes, loggedBy, clock.UtcNow);
        // Enrich the queued event with the vehicle reference so the broadcaster
        // can serialize the full action + vehicle without re-querying the DB.
        action.EnrichLastActionEventWith(vehicle);

        await actions.AddAsync(action, ct);
        var saveResult = await uow.SaveChangesAsync(ct);
        if (!saveResult.IsSuccess)
        {
            logger.LogVehicleActionFailed(c.VehicleId, saveResult.Message ?? "Save failed.", null);
            return Result<Guid>.Failure(saveResult.ErrorKind, saveResult.Message ?? "Save failed.");
        }

        logger.VehicleActionLogged(action.Id, action.VehicleId);
        return Result<Guid>.Success(action.Id);
    }
}
