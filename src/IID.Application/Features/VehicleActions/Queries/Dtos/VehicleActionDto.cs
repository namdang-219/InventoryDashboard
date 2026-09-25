namespace IID.Application.VehicleActions.Queries.Dtos;

public sealed record VehicleActionDto(
    Guid Id,
    Guid VehicleId,
    string ActionType,
    string? Notes,
    string LoggedByUserId,
    string LoggedByName,
    DateTimeOffset LoggedAtUtc,
    DateTimeOffset CreatedAtUtc,
    string? VehicleVin = null,
    string? VehicleMake = null,
    string? VehicleModel = null,
    int? VehicleYear = null,
    string? VehicleStockNumber = null);
