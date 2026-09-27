using IID.Application.Common.Interfaces;
using IID.Application.Logging;
using MediatR;

namespace IID.Application.Vehicles.Commands.UpdateVehicle;

/// <summary>
/// Persists a <see cref="Vehicle"/> mutation and lets the aggregate's
/// <see cref="Domain.Vehicles.Vehicle.Update"/> raised events
/// (<c>VehicleUpdated</c>, possibly <c>VehicleStatusChanged</c>) drive the
/// SignalR fan-out via <c>VehicleRealtimeEventHandler</c>. No direct reference
/// to <see cref="IVehicleHubNotifier"/> from this handler.
/// </summary>
public sealed class UpdateVehicleHandler(
    IVehicleRepository vehicles,
    IUnitOfWork uow,
    IClock clock,
    ICurrentUser currentUser,
    ILogger<UpdateVehicleHandler> logger) : IRequestHandler<UpdateVehicleCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(UpdateVehicleCommand c, CancellationToken ct)
    {
        var userId = currentUser.Id ?? currentUser.Email ?? "system";

        var vehicle = await vehicles.GetByIdAsync(c.Id, ct);
        if (vehicle is null)
        {
            logger.UpdateVehicleRejected(c.Id, "Vehicle not found.");
            return Result<Guid>.Failure(ErrorKind.NotFound, "Vehicle not found.");
        }

        if (vehicle.Status == VehicleStatus.Sold)
        {
            logger.UpdateVehicleRejected(c.Id, "Cannot edit a vehicle that has already been marked as sold.");
            return Result<Guid>.Failure(ErrorKind.Conflict, "Cannot edit a vehicle that has already been marked as sold.");
        }

        var purchase = Money.Of(c.PurchasePrice);
        var asking = Money.Of(c.AskingPrice);

        vehicle.Update(
            c.Make,
            c.Model,
            c.Year,
            c.Color,
            c.Mileage,
            c.FuelType,
            purchase,
            asking,
            c.Status,
            clock.UtcNow,
            userId);

        vehicles.Update(vehicle);
        var saveResult = await uow.SaveChangesAsync(ct);
        if (!saveResult.IsSuccess)
        {
            logger.UpdateVehicleFailed(c.Id, saveResult.Message ?? "Failed to save vehicle updates.", null);
            return Result<Guid>.Failure(saveResult.ErrorKind, saveResult.Message ?? "Failed to save vehicle updates.");
        }

        logger.VehicleUpdated(vehicle.Id, userId);
        return Result<Guid>.Success(vehicle.Id);
    }
}
