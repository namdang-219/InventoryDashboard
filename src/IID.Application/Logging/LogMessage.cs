namespace IID.Application.Logging;

/// <summary>
/// Centralized source-generated log messages for the Application layer.
/// All handlers should invoke these extension methods on their <see cref="ILogger"/>
/// rather than declaring private <c>[LoggerMessage]</c> methods inline.
/// </summary>
/// <remarks>
/// Naming convention:
/// <list type="bullet">
///   <item><c>*Succeeded</c> — Information, logged on the happy path right before <c>return</c>.</item>
///   <item><c>*Failed</c>    — Warning,  logged when a handler returns <c>Result.Failure(...)</c>
///                                  on a domain-level rejection; Error when persistence fails.</item>
///   <item><c>*Rejected</c>  — Warning,  logged when authorization / validation short-circuits the handler.</item>
///   <item><c>*NotFound</c>  — Warning,  logged when an entity lookup misses.</item>
/// </list>
/// Using source-generated partial methods keeps the hot path allocation-free
/// (no <see cref="string.Format(string, object[])"/> boxing of value-type args).
/// </remarks>
public static partial class LogMessage
{
    // ────────────────────────────────────────────────────────────────────
    // Vehicles › CreateVehicle
    // ────────────────────────────────────────────────────────────────────
    [LoggerMessage(Level = LogLevel.Information, Message = "Vehicle action logged {ActionId} on {VehicleId}")]
    public static partial void VehicleActionLogged(this ILogger logger, Guid actionId, Guid vehicleId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Vehicle created {VehicleId} VIN={Vin}")]
    public static partial void VehicleCreated(this ILogger logger, Guid vehicleId, string vin);

    [LoggerMessage(Level = LogLevel.Information, Message = "Vehicle sold {VehicleId} VIN={Vin} amount={Amount}")]
    public static partial void VehicleSold(this ILogger logger, Guid vehicleId, string vin, decimal amount);

    [LoggerMessage(Level = LogLevel.Warning, Message = "CreateVehicle rejected VIN={Vin} reason={Reason}")]
    public static partial void CreateVehicleRejected(this ILogger logger, string vin, string reason);

    [LoggerMessage(Level = LogLevel.Error, Message = "CreateVehicle failed to persist VIN={Vin}: {ErrorMessage}")]
    public static partial void CreateVehicleFailed(this ILogger logger, string vin, string errorMessage, Exception? ex);

    // ────────────────────────────────────────────────────────────────────
    // Vehicles › UpdateVehicle
    // ────────────────────────────────────────────────────────────────────
    [LoggerMessage(Level = LogLevel.Information, Message = "Vehicle updated {VehicleId} by {UserId}")]
    public static partial void VehicleUpdated(this ILogger logger, Guid vehicleId, string? userId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "UpdateVehicle rejected {VehicleId} reason={Reason}")]
    public static partial void UpdateVehicleRejected(this ILogger logger, Guid vehicleId, string reason);

    [LoggerMessage(Level = LogLevel.Error, Message = "UpdateVehicle failed to persist {VehicleId}: {ErrorMessage}")]
    public static partial void UpdateVehicleFailed(this ILogger logger, Guid vehicleId, string errorMessage, Exception? ex);

    // ────────────────────────────────────────────────────────────────────
    // Vehicles › MarkVehicleSold
    // ────────────────────────────────────────────────────────────────────
    [LoggerMessage(Level = LogLevel.Warning, Message = "MarkVehicleSold rejected {VehicleId} reason={Reason}")]
    public static partial void MarkVehicleSoldRejected(this ILogger logger, Guid vehicleId, string reason);

    [LoggerMessage(Level = LogLevel.Error, Message = "MarkVehicleSold failed to persist {VehicleId}: {ErrorMessage}")]
    public static partial void MarkVehicleSoldFailed(this ILogger logger, Guid vehicleId, string errorMessage, Exception? ex);

    // ────────────────────────────────────────────────────────────────────
    // Vehicles › TransferDealership
    // ────────────────────────────────────────────────────────────────────
    [LoggerMessage(Level = LogLevel.Information, Message = "Vehicle {VehicleId} transferred from dealership {OldDealer} to {NewDealer}")]
    public static partial void VehicleTransferred(this ILogger logger, Guid vehicleId, Guid? oldDealer, Guid newDealer);

    [LoggerMessage(Level = LogLevel.Warning, Message = "TransferDealership rejected {VehicleId} reason={Reason}")]
    public static partial void TransferDealershipRejected(this ILogger logger, Guid vehicleId, string reason);

    [LoggerMessage(Level = LogLevel.Error, Message = "TransferDealership failed to persist {VehicleId}: {ErrorMessage}")]
    public static partial void TransferDealershipFailed(this ILogger logger, Guid vehicleId, string errorMessage, Exception? ex);

    // ────────────────────────────────────────────────────────────────────
    // Vehicles › Queries
    // ────────────────────────────────────────────────────────────────────
    [LoggerMessage(Level = LogLevel.Warning, Message = "GetVehicleById not-found {VehicleId}")]
    public static partial void GetVehicleByIdNotFound(this ILogger logger, Guid vehicleId);

    // ────────────────────────────────────────────────────────────────────
    // VehicleActions › Log / Update / SoftDelete
    // ────────────────────────────────────────────────────────────────────
    [LoggerMessage(Level = LogLevel.Warning, Message = "LogVehicleAction rejected {VehicleId} reason={Reason}")]
    public static partial void LogVehicleActionRejected(this ILogger logger, Guid vehicleId, string reason);

    [LoggerMessage(Level = LogLevel.Error, Message = "LogVehicleAction failed to persist {VehicleId}: {ErrorMessage}")]
    public static partial void LogVehicleActionFailed(this ILogger logger, Guid vehicleId, string errorMessage, Exception? ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Vehicle action updated {ActionId} by {UserId}")]
    public static partial void VehicleActionUpdated(this ILogger logger, Guid actionId, string? userId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "UpdateVehicleAction rejected {ActionId} reason={Reason}")]
    public static partial void UpdateVehicleActionRejected(this ILogger logger, Guid actionId, string reason);

    [LoggerMessage(Level = LogLevel.Error, Message = "UpdateVehicleAction failed to persist {ActionId}: {ErrorMessage}")]
    public static partial void UpdateVehicleActionFailed(this ILogger logger, Guid actionId, string errorMessage, Exception? ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Vehicle action soft-deleted {ActionId} by {UserId}")]
    public static partial void VehicleActionSoftDeleted(this ILogger logger, Guid actionId, string? userId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "SoftDeleteVehicleAction rejected {ActionId} reason={Reason}")]
    public static partial void SoftDeleteVehicleActionRejected(this ILogger logger, Guid actionId, string reason);

    [LoggerMessage(Level = LogLevel.Error, Message = "SoftDeleteVehicleAction failed to persist {ActionId}: {ErrorMessage}")]
    public static partial void SoftDeleteVehicleActionFailed(this ILogger logger, Guid actionId, string errorMessage, Exception? ex);

    // ────────────────────────────────────────────────────────────────────
    // Activities › MarkRead
    // ────────────────────────────────────────────────────────────────────
    [LoggerMessage(Level = LogLevel.Information, Message = "Activities marked read by {UserId} mode={Mode} count={Count}")]
    public static partial void ActivitiesMarkedRead(this ILogger logger, string userId, string mode, int count);

    [LoggerMessage(Level = LogLevel.Warning, Message = "MarkActivityRead rejected reason={Reason}")]
    public static partial void MarkActivityReadRejected(this ILogger logger, string reason);

    // ────────────────────────────────────────────────────────────────────
    // Dealerships
    // ────────────────────────────────────────────────────────────────────
    [LoggerMessage(Level = LogLevel.Information, Message = "Created dealership {Id} ({Code} - {Name})")]
    public static partial void DealershipCreated(this ILogger logger, Guid id, string code, string name);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to persist dealership {Code}: {ErrorMessage}")]
    public static partial void CreateDealershipFailed(this ILogger logger, string code, string errorMessage, Exception? ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Updated dealership {Id} ({Code} - {Name})")]
    public static partial void DealershipUpdated(this ILogger logger, Guid id, string code, string name);

    [LoggerMessage(Level = LogLevel.Warning, Message = "UpdateDealership not-found {Id}")]
    public static partial void UpdateDealershipNotFound(this ILogger logger, Guid id);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to update dealership {Id}: {ErrorMessage}")]
    public static partial void UpdateDealershipFailed(this ILogger logger, Guid id, string errorMessage, Exception? ex);

    // ────────────────────────────────────────────────────────────────────
    // Auth › Login / Refresh
    // ────────────────────────────────────────────────────────────────────
    [LoggerMessage(Level = LogLevel.Information, Message = "Login succeeded for {Email} userId={UserId}")]
    public static partial void LoginSucceeded(this ILogger logger, string email, string userId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Login failed for {Email} reason={Reason}")]
    public static partial void LoginFailed(this ILogger logger, string email, string reason);

    [LoggerMessage(Level = LogLevel.Information, Message = "RefreshToken succeeded for userId={UserId}")]
    public static partial void RefreshTokenSucceeded(this ILogger logger, string userId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "RefreshToken failed reason={Reason}")]
    public static partial void RefreshTokenFailed(this ILogger logger, string reason);

    // ────────────────────────────────────────────────────────────────────
    // Dashboard › queries
    // ────────────────────────────────────────────────────────────────────
    [LoggerMessage(Level = LogLevel.Information, Message = "Dashboard summary generated: total={Total} available={Available} pending={Pending} sold={Sold} aging={Aging}")]
    public static partial void DashboardSummaryGenerated(this ILogger logger, int total, int available, int pending, int sold, int aging);

    [LoggerMessage(Level = LogLevel.Information, Message = "Dashboard bundle generated: total={Total} actionCenter={Actions} page={Page}")]
    public static partial void DashboardBundleGenerated(this ILogger logger, int total, int actions, int page);

    // ────────────────────────────────────────────────────────────────────
    // Dashboard › Service
    // ────────────────────────────────────────────────────────────────────
    [LoggerMessage(Level = LogLevel.Information, Message = "DashboardService.GetSummary generated: total={Total} available={Available} aging={Aging}")]
    public static partial void DashboardServiceSummaryGenerated(this ILogger logger, int total, int available, int aging);

    [LoggerMessage(Level = LogLevel.Information, Message = "DashboardService.GetAlerts generated: {Count}")]
    public static partial void DashboardServiceAlertsGenerated(this ILogger logger, int count);
}
