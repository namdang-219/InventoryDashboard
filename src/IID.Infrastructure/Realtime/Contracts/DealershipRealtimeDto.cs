namespace IID.Infrastructure.Realtime.Contracts;

/// <summary>
/// Realtime wire DTO for a single dealership pushed over SignalR.
/// Lives in Infrastructure to keep wire formats independent of
/// Application HTTP read DTOs. <c>VehicleCount</c> is intentionally
/// omitted from this DTO — it changes frequently as inventory
/// churns and clients pull it separately via the dashboard refresh
/// signal (<c>InventoryChanged</c> / <c>DashboardSummaryUpdated</c>).
/// </summary>
public sealed record DealershipRealtimeDto(
    Guid Id,
    string Name,
    string Code,
    string City,
    string State,
    string Phone);
