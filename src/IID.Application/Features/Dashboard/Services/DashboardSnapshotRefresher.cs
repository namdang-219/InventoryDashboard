using IID.Application.Common.Interfaces;
using IID.Application.Common.Messaging;

namespace IID.Application.Features.Dashboard.Services;

/// <summary>
/// Orchestrates computing, persisting, and publishing static dashboard snapshots
/// for startup bootstrap, scheduled aging refreshes, and coalesced Kafka mutation batches.
/// </summary>
public sealed class DashboardSnapshotRefresher(
    IDealershipRepository dealerships,
    IDashboardSnapshotCalculator calculator,
    IDashboardSnapshotRepository snapshots,
    IKafkaEventProducer producer,
    IClock clock,
    ILogger<DashboardSnapshotRefresher> logger) : IDashboardSnapshotRefresher
{
    public async Task<int> RefreshAllSnapshotsAsync(bool publishUpdates = false, CancellationToken ct = default)
    {
        var now = clock.UtcNow;
        var allDealerships = await dealerships.ListAsync(ct);

        var scopes = new List<Guid?>(allDealerships.Count + 1) { null };
        foreach (var dealership in allDealerships)
        {
            scopes.Add(dealership.Id);
        }

        var count = 0;
        foreach (var dealershipId in scopes)
        {
            await RefreshSingleScopeAsync(dealershipId, now, publishUpdates, ct);
            count++;
        }

        logger.LogInformation(
            "Refreshed {Count} static dashboard snapshot(s) (global + {DealershipCount} dealerships, publishUpdates={Publish}).",
            count,
            allDealerships.Count,
            publishUpdates);

        return count;
    }

    public async Task<int> RefreshForMutationsAsync(
        IReadOnlyCollection<VehicleMutatedIntegrationEvent> mutations,
        CancellationToken ct = default)
    {
        if (mutations.Count == 0)
            return 0;

        if (mutations.Any(m => string.Equals(m.MutationType, VehicleMutationTypes.Seeded, StringComparison.OrdinalIgnoreCase)))
        {
            return await RefreshAllSnapshotsAsync(publishUpdates: true, ct);
        }

        var affectedDealershipIds = new HashSet<Guid>();
        foreach (var mutation in mutations)
        {
            if (mutation.DealershipId.HasValue && mutation.DealershipId.Value != Guid.Empty)
                affectedDealershipIds.Add(mutation.DealershipId.Value);

            if (mutation.PreviousDealershipId.HasValue && mutation.PreviousDealershipId.Value != Guid.Empty)
                affectedDealershipIds.Add(mutation.PreviousDealershipId.Value);
        }

        var now = clock.UtcNow;
        var count = 0;

        // Regenerate each affected dealership scope first, then the global fleet scope
        foreach (var dealershipId in affectedDealershipIds)
        {
            await RefreshSingleScopeAsync(dealershipId, now, publishUpdates: true, ct);
            count++;
        }

        await RefreshSingleScopeAsync(dealershipId: null, now, publishUpdates: true, ct);
        count++;

        logger.LogInformation(
            "Processed {MutationCount} coalesced mutation(s) -> regenerated {ScopeCount} snapshot scope(s).",
            mutations.Count,
            count);

        return count;
    }

    private async Task RefreshSingleScopeAsync(
        Guid? dealershipId,
        DateTimeOffset asOfUtc,
        bool publishUpdates,
        CancellationToken ct)
    {
        var payload = await calculator.CalculateForScopeAsync(dealershipId, asOfUtc, ct);
        var saved = await snapshots.UpsertAsync(payload, ct);

        if (!publishUpdates)
            return;

        var updateEvent = new DashboardSnapshotUpdatedIntegrationEvent(
            Guid.NewGuid(),
            saved.ScopeKey,
            saved.DealershipId,
            payload.Summary,
            payload.RealtimeAlerts,
            saved.ComputedAtUtc,
            saved.Version);

        await producer.PublishAsync(KafkaTopics.DashboardUpdates, saved.ScopeKey, updateEvent, ct);
    }
}

