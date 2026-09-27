namespace IID.Infrastructure.Realtime.Contracts;

/// <summary>
/// Realtime wire DTO for a single vehicle pushed over SignalR.
/// Lives in Infrastructure to keep wire formats independent of
/// Application HTTP read DTOs.
/// </summary>
public sealed record VehicleRealtimeDto(
    Guid Id,
    string Vin,
    string Make,
    string Model,
    int Year,
    int DaysInInventory,
    bool IsAging,
    string Status,
    Guid DealershipId);
