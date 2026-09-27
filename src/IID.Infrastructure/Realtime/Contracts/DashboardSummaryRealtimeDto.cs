namespace IID.Infrastructure.Realtime.Contracts;

public sealed record DashboardSummaryRealtimeDto(
    DateTimeOffset GeneratedAtUtc,
    int TotalInventory,
    int AvailableCount,
    int PendingCount,
    int SoldCount,
    int WholesaleCount,
    int AgingCount,
    decimal TotalInventoryValue);
