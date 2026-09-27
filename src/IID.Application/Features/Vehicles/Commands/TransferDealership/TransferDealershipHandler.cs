using IID.Application.Common.Interfaces;
using IID.Application.Logging;
using IID.Domain.Common;
using IID.Domain.VehicleActions;
using MediatR;

namespace IID.Application.Vehicles.Commands.TransferDealership;

/// <summary>
/// Transfers a <see cref="Vehicle"/> between dealerships and writes an audit
/// <see cref="VehicleAction"/>. The <see cref="Vehicle.TransferDealership"/>
/// raised <c>VehicleTransferred</c> event and the
/// <see cref="VehicleAction.Log"/> raised <c>VehicleActionLogged</c> event are
/// each consumed by their dedicated event handlers, which route to SignalR with
/// the right scope (<see cref="Features.Vehicles.EventHandlers.VehicleTransferRealtimeEventHandler"/>
/// broadcasts to both dealer groups;
/// <see cref="Features.VehicleActions.EventHandlers.VehicleActionRealtimeEventHandler"/>
/// broadcasts to the action's vehicle group and triggers an inventory refresh).
/// No direct reference to <see cref="IVehicleHubNotifier"/> from this handler.
/// </summary>
public sealed class TransferDealershipHandler(
    IVehicleRepository vehicles,
    IDealershipRepository dealerships,
    IVehicleActionRepository actions,
    IUnitOfWork uow,
    IClock clock,
    ICurrentUser currentUser,
    ILogger<TransferDealershipHandler> logger) : IRequestHandler<TransferDealershipCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(TransferDealershipCommand c, CancellationToken ct)
    {
        var userId = currentUser.Id ?? currentUser.Email ?? "system";
        var vehicle = await vehicles.GetByIdAsync(c.VehicleId, ct);
        if (vehicle is null)
        {
            logger.TransferDealershipRejected(c.VehicleId, "Vehicle not found.");
            return Result<Guid>.Failure(ErrorKind.NotFound, "Vehicle not found.");
        }

        if (vehicle.Status == Domain.Vehicles.VehicleStatus.Sold)
        {
            logger.TransferDealershipRejected(c.VehicleId, "Cannot transfer a vehicle that has already been marked as sold.");
            return Result<Guid>.Failure(ErrorKind.Conflict, "Cannot transfer a vehicle that has already been marked as sold.");
        }

        if (vehicle.DealershipId == c.TargetDealershipId)
        {
            logger.TransferDealershipRejected(c.VehicleId, "Vehicle is already assigned to this dealership.");
            return Result<Guid>.Failure(ErrorKind.Conflict, "Vehicle is already assigned to this dealership.");
        }

        var targetDealer = await dealerships.GetByIdAsync(c.TargetDealershipId, ct);
        if (targetDealer is null)
        {
            logger.TransferDealershipRejected(c.VehicleId, "Target dealership not found.");
            return Result<Guid>.Failure(ErrorKind.NotFound, "Target dealership not found.");
        }

        var now = clock.UtcNow;

        vehicle.TransferDealership(c.TargetDealershipId, now, userId);
        vehicles.Update(vehicle);

        // Record audit activity
        var transferNote = $"Transferred to {targetDealer.Name} ({targetDealer.City}, {targetDealer.State})." +
            (string.IsNullOrWhiteSpace(c.Notes) ? "" : $" Reason: {c.Notes.Trim()}");

        var action = VehicleAction.Log(
            vehicle.Id,
            VehicleActionType.TransferDealership,
            transferNote,
            userId,
            now);
        // Enrich the audit-action event so the broadcaster can serialize both
        // the action and the (now-reassigned) vehicle without re-querying.
        action.EnrichLastActionEventWith(vehicle);

        await actions.AddAsync(action, ct);

        var saveResult = await uow.SaveChangesAsync(ct);
        if (!saveResult.IsSuccess)
        {
            logger.TransferDealershipFailed(c.VehicleId, saveResult.Message ?? "Failed to save dealership transfer.", null);
            return Result<Guid>.Failure(saveResult.ErrorKind, saveResult.Message ?? "Failed to save dealership transfer.");
        }

        logger.VehicleTransferred(vehicle.Id, vehicle.DealershipId, targetDealer.Id);

        return Result<Guid>.Success(vehicle.Id);
    }
}
