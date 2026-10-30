using IID.Application.Common.Interfaces;
using IID.Application.Dashboard.Queries.Dtos;
using IID.Application.Features.Dashboard.Services;
using IID.Domain.Dashboard;
using MediatR;

namespace IID.Application.Dashboard.Queries.GetLowInventoryAlerts;

public sealed class GetLowInventoryAlertsHandler(
    IDashboardSnapshotRepository snapshots,
    IDashboardSnapshotCalculator calculator,
    ILogger<GetLowInventoryAlertsHandler> logger) : IRequestHandler<GetLowInventoryAlertsQuery, Result<IReadOnlyList<LowInventoryAlertDto>>>
{
    public async Task<Result<IReadOnlyList<LowInventoryAlertDto>>> Handle(
        GetLowInventoryAlertsQuery q, CancellationToken ct)
    {
        var dealershipId = q.DealershipId.HasValue && q.DealershipId.Value != Guid.Empty
            ? q.DealershipId
            : null;
        var scopeKey = DashboardSnapshot.BuildScopeKey(dealershipId);

        var snapshot = await snapshots.GetByScopeAsync(scopeKey, ct);
        if (snapshot is not null)
        {
            return Result<IReadOnlyList<LowInventoryAlertDto>>.Success(
                DashboardSnapshotSerializer.ReadLowInventoryAlerts(snapshot));
        }

        logger.LogWarning(
            "Static low-inventory alerts snapshot not found for scope {ScopeKey}; computing on-demand fallback.",
            scopeKey);

        var fallback = await calculator.CalculateForScopeAsync(dealershipId, ct: ct);
        return Result<IReadOnlyList<LowInventoryAlertDto>>.Success(fallback.LowInventoryAlerts);
    }
}
