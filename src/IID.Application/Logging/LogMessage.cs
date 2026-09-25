namespace IID.Application.Logging;

/// <summary>
/// Centralized source-generated log messages for the Application layer.
/// All handlers should invoke these extension methods on their <see cref="ILogger"/>
/// rather than declaring private <c>[LoggerMessage]</c> methods inline.
/// </summary>
public static partial class LogMessage
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Vehicle action logged {ActionId} on {VehicleId}")]
    public static partial void VehicleActionLogged(this ILogger logger, Guid actionId, Guid vehicleId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Vehicle created {VehicleId} VIN={Vin}")]
    public static partial void VehicleCreated(this ILogger logger, Guid vehicleId, string vin);

    [LoggerMessage(Level = LogLevel.Information, Message = "Vehicle sold {VehicleId} VIN={Vin} amount={Amount}")]
    public static partial void VehicleSold(this ILogger logger, Guid vehicleId, string vin, decimal amount);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Listed vehicles total={Total} page={Page} limit={Limit}")]
    public static partial void VehiclesListed(this ILogger logger, int total, int page, int limit);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Fetched aging-stock page={Page} limit={Limit} total={Total}")]
    public static partial void AgingStockFetched(this ILogger logger, int total, int page, int limit);
}
