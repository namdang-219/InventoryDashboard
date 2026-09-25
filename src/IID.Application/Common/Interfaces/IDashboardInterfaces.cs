using IID.Application.Dashboard.Dtos;

namespace IID.Application.Common.Interfaces;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken ct);
    Task<IReadOnlyList<DashboardAlertDto>> GetAlertsAsync(CancellationToken ct);
}
