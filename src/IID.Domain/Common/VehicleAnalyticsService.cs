using IID.Domain.Vehicles;

namespace IID.Domain.Common;

/// <summary>
/// Rich aging-stock service. Replaces the old AgingStockIdentifier with full severity + demand analytics.
/// Backward-compatible: IsAging/DaysInInventory preserved.
/// </summary>
public sealed class VehicleAnalyticsService(DateTimeOffset nowUtc)
{
    /// <summary>Threshold above which a vehicle is considered aging (in days).</summary>
    public const int DefaultAgingThresholdDays = 90;

    public bool IsAging(Vehicle vehicle) => vehicle.IsAging(DefaultAgingThresholdDays);

    public int DaysInInventory(Vehicle vehicle) => vehicle.DaysInInventory(nowUtc);

    public AgingSeverity GetAgingSeverity(Vehicle vehicle) => vehicle.GetAgingSeverity(nowUtc);

    public int GetDemandScore(Vehicle vehicle) => vehicle.GetDemandScore(nowUtc);

    public DemandLevel GetDemandLevel(Vehicle vehicle) => vehicle.GetDemandLevel(nowUtc);

    public IReadOnlyDictionary<AgingSeverity, int> BucketByAging(IEnumerable<Vehicle> vehicles)
    {
        var buckets = new Dictionary<AgingSeverity, int>
        {
            [AgingSeverity.None] = 0,
            [AgingSeverity.Warning] = 0,
            [AgingSeverity.High] = 0,
            [AgingSeverity.Critical] = 0
        };

        foreach (var v in vehicles)
        {
            var severity = GetAgingSeverity(v);
            buckets[severity]++;
        }

        return buckets;
    }
}
