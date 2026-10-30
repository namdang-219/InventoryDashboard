using IID.Application.Common.Interfaces;
using IID.Application.Features.Dashboard.Services;
using IID.Domain.Dashboard;

namespace IID.Infrastructure.Persistence.Repositories;

public sealed class DashboardSnapshotRepository(IidDbContext db) : IDashboardSnapshotRepository
{
    public async Task<DashboardSnapshot?> GetByScopeAsync(string scopeKey, CancellationToken ct = default)
        => await db.DashboardSnapshots
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ScopeKey == scopeKey, ct);

    public async Task<DashboardSnapshot> UpsertAsync(DashboardSnapshotPayload payload, CancellationToken ct = default)
    {
        var summaryJson = DashboardSnapshotSerializer.Serialize(payload.Summary);
        var quickStatsJson = DashboardSnapshotSerializer.Serialize(payload.QuickStats);
        var chartsJson = DashboardSnapshotSerializer.Serialize(payload.Charts);
        var actionCenterJson = DashboardSnapshotSerializer.Serialize(payload.ActionCenter);
        var aiInsightsJson = DashboardSnapshotSerializer.Serialize(payload.AiInsights);
        var lowInventoryAlertsJson = DashboardSnapshotSerializer.Serialize(payload.LowInventoryAlerts);
        var realtimeAlertsJson = DashboardSnapshotSerializer.Serialize(payload.RealtimeAlerts);

        var existing = await db.DashboardSnapshots
            .FirstOrDefaultAsync(x => x.ScopeKey == payload.ScopeKey, ct);

        if (existing is not null)
        {
            existing.Update(
                summaryJson,
                quickStatsJson,
                chartsJson,
                actionCenterJson,
                aiInsightsJson,
                lowInventoryAlertsJson,
                realtimeAlertsJson,
                payload.ComputedAtUtc);

            await db.SaveChangesAsync(ct);
            return existing;
        }

        var created = DashboardSnapshot.Create(
            payload.DealershipId,
            summaryJson,
            quickStatsJson,
            chartsJson,
            actionCenterJson,
            aiInsightsJson,
            lowInventoryAlertsJson,
            realtimeAlertsJson,
            payload.ComputedAtUtc);

        await db.DashboardSnapshots.AddAsync(created, ct);

        try
        {
            await db.SaveChangesAsync(ct);
            return created;
        }
        catch (DbUpdateException)
        {
            // Handle rare concurrent initial insert on the same ScopeKey
            db.Entry(created).State = EntityState.Detached;
            var concurrent = await db.DashboardSnapshots
                .FirstAsync(x => x.ScopeKey == payload.ScopeKey, ct);

            concurrent.Update(
                summaryJson,
                quickStatsJson,
                chartsJson,
                actionCenterJson,
                aiInsightsJson,
                lowInventoryAlertsJson,
                realtimeAlertsJson,
                payload.ComputedAtUtc);

            await db.SaveChangesAsync(ct);
            return concurrent;
        }
    }
}

