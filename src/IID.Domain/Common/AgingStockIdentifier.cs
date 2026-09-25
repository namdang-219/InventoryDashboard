using IID.Domain.Vehicles;

namespace IID.Domain.Common;

/// <summary>
/// Domain service that identifies aging stock from the vehicle aggregate.
/// </summary>
public sealed class AgingStockIdentifier(DateTimeOffset nowUtc)
{
    public bool IsAging(Vehicle vehicle) =>
        (nowUtc - vehicle.DateAddedToInventory).TotalDays > InventoryPolicy.AgingStockThresholdDays;

    public int DaysInInventory(Vehicle vehicle) =>
        Math.Max(0, (int)(nowUtc - vehicle.DateAddedToInventory).TotalDays);
}
