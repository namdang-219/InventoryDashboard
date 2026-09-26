using System;
using System.Threading;
using System.Threading.Tasks;
using IID.Application.Common.Interfaces;
using MediatR;
namespace IID.Application.Vehicles.Commands.UpdateVehicle;

public sealed class UpdateVehicleHandler(
    IVehicleRepository vehicles,
    IUnitOfWork uow,
    IClock clock,
    ICurrentUser currentUser,
    IVehicleHubNotifier notifier) : IRequestHandler<UpdateVehicleCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(UpdateVehicleCommand c, CancellationToken ct)
    {
        var vehicle = await vehicles.GetByIdAsync(c.Id, ct);
        if (vehicle is null)
            return Result<Guid>.Failure(ErrorKind.NotFound, "Vehicle not found.");

        if (vehicle.Status == VehicleStatus.Sold)
            return Result<Guid>.Failure(ErrorKind.Conflict, "Cannot edit a vehicle that has already been marked as sold.");

        var purchase = Money.Of(c.PurchasePrice);
        var asking = Money.Of(c.AskingPrice);
        var userId = currentUser.Id ?? currentUser.Email ?? "system";

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
            return Result<Guid>.Failure(saveResult.ErrorKind, saveResult.Message ?? "Failed to save vehicle updates.");

        await notifier.VehicleUpdatedAsync(vehicle, ct);

        return Result<Guid>.Success(vehicle.Id);
    }
}
