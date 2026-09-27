using IID.Application.Dashboard.Dtos;
using IID.Domain.Dealerships;
using IID.Domain.Vehicles;
using IID.Domain.VehicleActions;

namespace IID.Infrastructure.Realtime.Contracts;

/// <summary>
/// Maps Application-layer service DTOs into Infrastructure-owned
/// realtime wire DTOs. Decouples the wire contract from HTTP read DTOs.
/// </summary>
public interface IRealtimeContractMapper
{
    VehicleRealtimeDto MapVehicle(Vehicle v);
    VehicleActionRealtimeDto MapVehicleAction(VehicleAction a, Vehicle? v);
    DashboardSummaryRealtimeDto MapSummary(DashboardSummaryDto dto);
    DashboardAlertRealtimeDto MapAlert(DashboardAlertDto dto);
    DealershipRealtimeDto MapDealership(Dealership d);
}

public sealed class RealtimeContractMapper : IRealtimeContractMapper
{
    public VehicleRealtimeDto MapVehicle(Vehicle v)
    {
        var analytics = new VehicleAnalyticsService(DateTimeOffset.UtcNow);
        return new VehicleRealtimeDto(
            v.Id, v.Vin.Value, v.Make, v.Model, v.Year,
            analytics.DaysInInventory(v), analytics.IsAging(v),
            v.Status.ToString(), v.DealershipId);
    }

    public VehicleActionRealtimeDto MapVehicleAction(VehicleAction a, Vehicle? v)
    {
        var vehicleName = v is not null ? $"{v.Year} {v.Make} {v.Model}" : null;
        return new VehicleActionRealtimeDto(a.Id, a.VehicleId, a.ActionType.ToString(), a.Notes, a.LoggedAt, vehicleName);
    }

    public DashboardSummaryRealtimeDto MapSummary(DashboardSummaryDto dto) =>
        new(dto.GeneratedAtUtc, dto.TotalInventory, dto.AvailableCount,
            dto.PendingCount, dto.SoldCount, dto.WholesaleCount, dto.AgingCount, dto.TotalInventoryValue);

    public DashboardAlertRealtimeDto MapAlert(DashboardAlertDto dto) =>
        new(dto.VehicleId, dto.Message, dto.Severity, dto.CreatedAtUtc);

    public DealershipRealtimeDto MapDealership(Dealership d) =>
        new(d.Id, d.Name, d.Code, d.City, d.State, d.Phone);
}
