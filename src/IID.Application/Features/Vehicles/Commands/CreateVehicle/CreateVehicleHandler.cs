using IID.Application.Common.Interfaces;
using IID.Application.Logging;
using IID.Application.Vehicles.Queries.Dtos;
using MediatR;

namespace IID.Application.Vehicles.Commands.CreateVehicle;

public sealed class CreateVehicleHandler(
    IVehicleRepository vehicles,
    IUnitOfWork uow,
    IVehicleHubNotifier notifier,
    IClock clock,
    ICurrentUser currentUser,
    ILogger<CreateVehicleHandler> logger) : IRequestHandler<CreateVehicleCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateVehicleCommand c, CancellationToken ct)
    {
        var vin = Vin.Parse(c.Vin);
        if (await vehicles.VinExistsAsync(vin.Value, ct))
            return Result<Guid>.Failure(ErrorKind.Conflict, "VIN already exists.");

        if (!string.IsNullOrWhiteSpace(c.StockNumber) && await vehicles.StockNumberExistsAsync(c.StockNumber, ct))
            return Result<Guid>.Failure(ErrorKind.Conflict, "StockNumber already exists.");

        var purchase = Money.Of(c.PurchasePrice);
        var asking = Money.Of(c.AskingPrice);
        var vehicle = Vehicle.Create(
            vin, c.Make, c.Model, c.Year, c.Color,
            c.Mileage, c.FuelType, purchase, asking, c.Status,
            c.DateAddedToInventory, clock.UtcNow, currentUser.Id, c.StockNumber, c.DealershipId);

        await vehicles.AddAsync(vehicle, ct);
        var saveResult = await uow.SaveChangesAsync(ct);
        if (!saveResult.IsSuccess)
            return Result<Guid>.Failure(saveResult.ErrorKind, saveResult.Message ?? "Save failed.");

        logger.VehicleCreated(vehicle.Id, vehicle.Vin.Value);

        await notifier.VehicleAddedAsync(vehicle, ct);
        return Result<Guid>.Success(vehicle.Id);
    }
}
