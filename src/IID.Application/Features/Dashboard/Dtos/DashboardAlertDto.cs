namespace IID.Application.Dashboard.Dtos;

public sealed record DashboardAlertDto(
    Guid? VehicleId,
    string Message,
    string Severity,
    DateTimeOffset CreatedAtUtc);
