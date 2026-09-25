using IID.Application.Common.Interfaces;
using IID.Application.Common.Models;
using IID.Application.Dashboard.Dtos;

namespace IID.Application.Features.Dashboard.Services;

public sealed class DashboardService : IDashboardService
{
    private readonly IVehicleRepository _vehicles;
    private readonly IClock _clock;

    public DashboardService(IVehicleRepository vehicles, IClock clock)
    {
        _vehicles = vehicles;
        _clock = clock;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var (items, _) = await _vehicles.ListAsync(
            new VehicleListFilter(Limit: 1000, Sort: "dateAdded", Order: "desc"), cancellationToken);

        var available = items.Count(v => v.Status == VehicleStatus.Available);
        var pending = items.Count(v => v.Status == VehicleStatus.Pending);
        var sold = items.Count(v => v.Status == VehicleStatus.Sold);
        var wholesale = items.Count(v => v.Status == VehicleStatus.Wholesale);
        var agingCount = items.Count(v => v.IsAging());
        var totalValue = items.Sum(v => v.AskingPrice.Amount);

        return new DashboardSummaryDto(
            _clock.UtcNow,
            items.Count,
            available,
            pending,
            sold,
            wholesale,
            agingCount,
            new Dictionary<string, int>(),
            totalValue,
            0m,
            0,
            new List<MakeDistributionDto>(),
            new List<FuelTypeDistributionDto>(),
            new List<DemandLevelDto>());
    }

    public async Task<IReadOnlyList<DashboardAlertDto>> GetAlertsAsync(CancellationToken cancellationToken)
    {
        var (items, _) = await _vehicles.ListAsync(
            new VehicleListFilter(Limit: 1000, Sort: "dateAdded", Order: "desc"), cancellationToken);

        var alerts = new List<DashboardAlertDto>();
        var now = _clock.UtcNow;

        foreach (var v in items.Where(v => v.Status == VehicleStatus.Available))
        {
            var severity = v.GetAgingSeverity(now);
            if (severity >= AgingSeverity.Warning)
            {
                var alertMsg = severity switch
                {
                    AgingSeverity.Critical => $"{v.Make} has been in stock for {v.DaysInInventory(now)} days (CRITICAL)",
                    AgingSeverity.High => $"{v.Make} has been in stock for {v.DaysInInventory(now)} days (HIGH)",
                    _ => $"{v.Make} has been in stock for {v.DaysInInventory(now)} days"
                };

                alerts.Add(new DashboardAlertDto(v.Id, alertMsg, severity.ToString(), now));
            }
        }

        return alerts.OrderByDescending(a => a.CreatedAtUtc).Take(10).ToList();
    }
}
