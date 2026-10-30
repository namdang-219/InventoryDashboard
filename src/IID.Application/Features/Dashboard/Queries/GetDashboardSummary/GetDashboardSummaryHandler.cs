using IID.Application.Common.Interfaces;
using IID.Application.Dashboard.Dtos;
using IID.Application.Features.Dashboard.Services;
using IID.Application.Logging;
using IID.Domain.Dashboard;
using MediatR;

namespace IID.Application.Dashboard.Queries.GetDashboardSummary;

public sealed class GetDashboardSummaryHandler(
    IDashboardSnapshotRepository snapshots,
    IDashboardSnapshotCalculator calculator,
    ILogger<GetDashboardSummaryHandler> logger) : IRequestHandler<GetDashboardSummaryQuery, Result<DashboardSummaryDto>>
{
    public async Task<Result<DashboardSummaryDto>> Handle(GetDashboardSummaryQuery q, CancellationToken ct)
    {
        DashboardSummaryDto summary;

        if (q.AsOfUtc is null)
        {
            var snapshot = await snapshots.GetByScopeAsync(DashboardSnapshot.GlobalScopeKey, ct);
            summary = snapshot is not null
                ? DashboardSnapshotSerializer.ReadSummary(snapshot)
                : (await calculator.CalculateForScopeAsync(null, null, ct)).Summary;
        }
        else
        {
            summary = (await calculator.CalculateForScopeAsync(null, q.AsOfUtc, ct)).Summary;
        }

        logger.DashboardSummaryGenerated(
            summary.TotalInventory,
            summary.AvailableCount,
            summary.PendingCount,
            summary.SoldCount,
            summary.AgingCount);

        return Result<DashboardSummaryDto>.Success(summary);
    }
}
