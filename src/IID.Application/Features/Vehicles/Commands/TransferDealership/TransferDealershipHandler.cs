using IID.Application.Common.Interfaces;
using IID.Domain.Common;
using IID.Domain.VehicleActions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IID.Application.Vehicles.Commands.TransferDealership;

public sealed class TransferDealershipHandler(
    IVehicleRepository vehicles,
    IDealershipRepository dealerships,
    IVehicleActionRepository actions,
    IUnitOfWork uow,
    IVehicleHubNotifier notifier,
    IClock clock,
    ICurrentUser currentUser,
    ILogger<TransferDealershipHandler> logger) : IRequestHandler<TransferDealershipCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(TransferDealershipCommand c, CancellationToken ct)
    {
        var vehicle = await vehicles.GetByIdAsync(c.VehicleId, ct);
        if (vehicle is null)
            return Result<Guid>.Failure(ErrorKind.NotFound, "Vehicle not found.");

        if (vehicle.Status == Domain.Vehicles.VehicleStatus.Sold)
            return Result<Guid>.Failure(ErrorKind.Conflict, "Cannot transfer a vehicle that has already been marked as sold.");

        if (vehicle.DealershipId == c.TargetDealershipId)
            return Result<Guid>.Failure(ErrorKind.Conflict, "Vehicle is already assigned to this dealership.");

        var targetDealer = await dealerships.GetByIdAsync(c.TargetDealershipId, ct);
        if (targetDealer is null)
            return Result<Guid>.Failure(ErrorKind.NotFound, "Target dealership not found.");

        var now = clock.UtcNow;
        var userId = currentUser.Id ?? currentUser.Email ?? "system";
        var oldDealerId = vehicle.DealershipId;

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

        await actions.AddAsync(action, ct);

        var saveResult = await uow.SaveChangesAsync(ct);
        if (!saveResult.IsSuccess)
            return Result<Guid>.Failure(saveResult.ErrorKind, saveResult.Message ?? "Failed to save dealership transfer.");

        logger.LogInformation("Vehicle {VehicleId} transferred from dealership {OldDealer} to {NewDealer}.",
            vehicle.Id, oldDealerId, targetDealer.Id);

        // Broadcast to the source (A) and target (B) dealership groups only.
        // No global broadcast — only the two affected dealerships need to refresh.
        await notifier.VehicleTransferredAsync(vehicle, oldDealerId, ct);
        await notifier.VehicleActionLoggedAsync(action, vehicle, ct);

        return Result<Guid>.Success(vehicle.Id);
    }
}
