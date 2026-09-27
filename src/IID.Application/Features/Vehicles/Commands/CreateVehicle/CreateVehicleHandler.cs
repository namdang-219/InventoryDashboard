using IID.Application.Common.Interfaces;
using IID.Application.Logging;
using MediatR;

namespace IID.Application.Vehicles.Commands.CreateVehicle;

/// <summary>
/// Persists a new <see cref="Vehicle"/> and lets the aggregate's domain events
/// (raised from <see cref="Vehicle.Create"/> and dispatched by
/// <c>DomainEventDispatchInterceptor</c>) handle the SignalR fan-out via
/// <c>VehicleRealtimeEventHandler</c>. This handler no longer references
/// <see cref="IVehicleHubNotifier"/> directly.
/// </summary>
public sealed class CreateVehicleHandler(
    IVehicleRepository vehicles,
    IUnitOfWork uow,
    IClock clock,
    ICurrentUser currentUser,
    ILogger<CreateVehicleHandler> logger) : IRequestHandler<CreateVehicleCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateVehicleCommand c, CancellationToken ct)
    {
        var vin = Vin.Parse(c.Vin);

        if (await vehicles.VinExistsAsync(vin.Value, ct))
        {
            logger.CreateVehicleRejected(vin.Value, "VIN already exists.");
            return Result<Guid>.Failure(ErrorKind.Conflict, "VIN already exists.");
        }

        if (!string.IsNullOrWhiteSpace(c.StockNumber) && await vehicles.StockNumberExistsAsync(c.StockNumber, ct))
        {
            logger.CreateVehicleRejected(vin.Value, $"StockNumber '{c.StockNumber}' already exists.");
            return Result<Guid>.Failure(ErrorKind.Conflict, "StockNumber already exists.");
        }

        var purchase = Money.Of(c.PurchasePrice);
        var asking = Money.Of(c.AskingPrice);
        var vehicle = Vehicle.Create(
            vin, c.Make, c.Model, c.Year, c.Color,
            c.Mileage, c.FuelType, purchase, asking, c.Status,
            c.DateAddedToInventory, clock.UtcNow, currentUser.Id, c.StockNumber, c.DealershipId);

        await vehicles.AddAsync(vehicle, ct);
        var saveResult = await uow.SaveChangesAsync(ct);
        if (!saveResult.IsSuccess)
        {
            logger.CreateVehicleFailed(vin.Value, saveResult.Message ?? "Save failed.", null);
            return Result<Guid>.Failure(saveResult.ErrorKind, saveResult.Message ?? "Save failed.");
        }

        logger.VehicleCreated(vehicle.Id, vehicle.Vin.Value);
        return Result<Guid>.Success(vehicle.Id);
    }
}
