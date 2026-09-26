using IID.Application.Common.Interfaces;
using IID.Application.Logging;
using MediatR;

namespace IID.Application.Vehicles.Commands.MarkVehicleSold;

public sealed class MarkVehicleSoldHandler(
    IVehicleRepository vehicles,
    IUnitOfWork uow,
    IVehicleHubNotifier notifier,
    IClock clock,
    ICurrentUser currentUser,
    ILogger<MarkVehicleSoldHandler> logger) : IRequestHandler<MarkVehicleSoldCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(MarkVehicleSoldCommand c, CancellationToken ct)
    {
        var vehicle = await vehicles.GetByIdAsync(c.VehicleId, ct);
        if (vehicle is null)
            return Result<Guid>.Failure(ErrorKind.NotFound, "Vehicle not found.");

        if (vehicle.Status == VehicleStatus.Sold)
            return Result<Guid>.Failure(ErrorKind.Conflict, "Vehicle is already sold.");

        // Apply caller's RowVersion onto the tracked entity so EF emits
        // WHERE RowVersion = @original on UPDATE. If another writer already
        // bumped the row, the UPDATE affects 0 rows and the Infrastructure
        // UnitOfWork translates that into ErrorKind.Conflict below.
        if (!string.IsNullOrWhiteSpace(c.RowVersionBase64))
        {
            try
            {
                var rowVersionBytes = Convert.FromBase64String(c.RowVersionBase64);
                vehicle.SetRowVersion(rowVersionBytes);
                vehicles.SetRowVersion(vehicle, rowVersionBytes);
            }
            catch (FormatException)
            {
                return Result<Guid>.Failure(
                    ErrorKind.ValidationFailed,
                    "rowVersion must be a valid Base64 string.");
            }
        }
        else if (c.RowVersion is { Length: > 0 })
        {
            vehicle.SetRowVersion(c.RowVersion);
            vehicles.SetRowVersion(vehicle, c.RowVersion);
        }

        var currency = c.SoldPriceCurrency ?? vehicle.AskingPrice.Currency;
        var soldPrice = Money.Of(c.SoldPrice, currency);
        var soldAt = c.SoldAtUtc ?? clock.UtcNow;

        vehicle.MarkSold(soldPrice, soldAt, currentUser.Id ?? "system");

        var saveResult = await uow.SaveChangesAsync(ct);
        if (!saveResult.IsSuccess)
            return Result<Guid>.Failure(saveResult.ErrorKind, saveResult.Message ?? "Save failed.");

        logger.VehicleSold(vehicle.Id, vehicle.Vin.Value, soldPrice.Amount);
        await notifier.VehicleUpdatedAsync(vehicle, ct);

        return Result<Guid>.Success(vehicle.Id);
    }
}
