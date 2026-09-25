using IID.Application.Common.Interfaces;
using IID.Application.Common.Models;
using IID.Application.Dashboard.Queries.Dtos;
using MediatR;
namespace IID.Application.Dashboard.Queries.GetLowInventoryAlerts;

public sealed class GetLowInventoryAlertsHandler(
    IVehicleRepository vehicles,
    ILogger<GetLowInventoryAlertsHandler> logger) : IRequestHandler<GetLowInventoryAlertsQuery, Result<IReadOnlyList<LowInventoryAlertDto>>>
{
    // Threshold below which a make/model combo is considered "low inventory"
    private const int LowInventoryThreshold = 3;

    public async Task<Result<IReadOnlyList<LowInventoryAlertDto>>> Handle(
        GetLowInventoryAlertsQuery q, CancellationToken ct)
    {
        const int fetchSize = 1000;
        var (items, _) = await vehicles.ListAsync(
            new VehicleListFilter(
                Status: VehicleStatus.Available,
                Limit: fetchSize,
                Sort: "dateAdded",
                Order: "desc",
                DealershipId: q.DealershipId), ct);

        // Group by make + model, flag combos with critically low availability.
        // Use a deterministic Guid derived from (Make|Model) so the same combo
        // always returns the same Id — keeps Angular @for tracks stable across
        // refreshes and prevents DOM thrash / disappearing rows.
        var alerts = items
            .GroupBy(v => (v.Make, v.Model))
            .Where(g => g.Count() < LowInventoryThreshold)
            .Select(g => new LowInventoryAlertDto(
                MakeModelAlertId(g.Key.Make, g.Key.Model),
                g.Key.Make,
                g.Key.Model,
                g.Count(),
                LowInventoryThreshold))
            .OrderByDescending(a => LowInventoryThreshold - a.Available) // most critical first
            .Take(10)
            .ToList();

        if (logger.IsEnabled(LogLevel.Debug))
            logger.LogDebug("Low-inventory alerts: {Count}", alerts.Count);

        return Result<IReadOnlyList<LowInventoryAlertDto>>.Success(alerts);
    }

    /// <summary>
    /// Deterministic, namespace-scoped Guid for a (make, model) pair.
    /// Two distinct calls produce the same Id, so the Angular @for `track`
    /// can match rows across page reloads and realtime updates.
    /// </summary>
    private static Guid MakeModelAlertId(string make, string model)
    {
        var seed = $"iid:low-inventory-alert:{make}:{model}";
        var bytes = System.Text.Encoding.UTF8.GetBytes(seed);
        var hash = System.Security.Cryptography.MD5.HashData(bytes);
        return new Guid(hash);
    }
}
