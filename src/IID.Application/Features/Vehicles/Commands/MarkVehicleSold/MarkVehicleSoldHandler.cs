using IID.Application.Common.Interfaces;
using IID.Application.Logging;
using MediatR;

namespace IID.Application.Vehicles.Commands.MarkVehicleSold;

/// <summary>
/// Marks a <see cref="Vehicle"/> as sold; the <see cref="Vehicle.MarkSold"/>
/// domain events (<c>VehicleStatusChanged</c> and <c>VehicleSold</c>) drive the
/// SignalR fan-out via <c>VehicleRealtimeEventHandler</c>. No direct reference
/// to <see cref="IVehicleHubNotifier"/> from this handler.
/// </summary>
public sealed class MarkVehicleSoldHandler(
    IVehicleRepository vehicles,
    IUnitOfWork uow,
    IClock clock,
    ICurrentUser currentUser,
    ILogger<MarkVehicleSoldHandler> logger) : IRequestHandler<MarkVehicleSoldCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(MarkVehicleSoldCommand c, CancellationToken ct)
    {
        var userId = currentUser.Id ?? "system";
        var currency = c.SoldPriceCurrency ?? "USD";

        var vehicle = await vehicles.GetByIdAsync(c.VehicleId, ct);
        if (vehicle is null)
        {
            logger.MarkVehicleSoldRejected(c.VehicleId, "Vehicle not found.");
            return Result<Guid>.Failure(ErrorKind.NotFound, "Vehicle not found.");
        }

        if (vehicle.Status == VehicleStatus.Sold)
        {
            logger.MarkVehicleSoldRejected(c.VehicleId, "Vehicle is already sold.");
            return Result<Guid>.Failure(ErrorKind.Conflict, "Vehicle is already sold.");
        }

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
                logger.MarkVehicleSoldRejected(c.VehicleId, "rowVersion must be a valid Base64 string.");
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

        var actualCurrency = c.SoldPriceCurrency ?? vehicle.AskingPrice.Currency;
        var soldPrice = Money.Of(c.SoldPrice, actualCurrency);
        var soldAt = c.SoldAtUtc ?? clock.UtcNow;

        vehicle.MarkSold(soldPrice, soldAt, currentUser.Id ?? "system");

        var saveResult = await uow.SaveChangesAsync(ct);
        if (!saveResult.IsSuccess)
        {
            logger.MarkVehicleSoldFailed(c.VehicleId, saveResult.Message ?? "Save failed.", null);
            return Result<Guid>.Failure(saveResult.ErrorKind, saveResult.Message ?? "Save failed.");
        }

        logger.VehicleSold(vehicle.Id, vehicle.Vin.Value, soldPrice.Amount);
        return Result<Guid>.Success(vehicle.Id);
    }
}
