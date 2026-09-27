namespace IID.Infrastructure.Realtime.Contracts;

public sealed record VehicleActionRealtimeDto(
    Guid Id,
    Guid VehicleId,
    string ActionType,
    string? Notes,
    DateTimeOffset LoggedAtUtc,
    string? VehicleName = null);
