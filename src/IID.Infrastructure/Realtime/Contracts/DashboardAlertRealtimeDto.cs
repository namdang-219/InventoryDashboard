namespace IID.Infrastructure.Realtime.Contracts;

public sealed record DashboardAlertRealtimeDto(
    Guid? VehicleId,
    string Message,
    string Severity,
    DateTimeOffset CreatedAtUtc);
