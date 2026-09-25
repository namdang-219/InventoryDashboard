using IID.Application.Dashboard.Dtos;

namespace IID.Application.Dashboard.Queries.Dtos;

/// <summary>
/// Full dashboard bundle returned by GET /api/v1/dashboard.
/// Includes summary, computed quick-stats, chart data, action items, and a paginated
/// inventory slice — everything a dashboard page needs in a single round-trip.
/// </summary>
public sealed record DashboardBundleDto(
    DashboardSummaryDto Summary,
    QuickStatsDto QuickStats,
    InventoryChartsDto Charts,
    IReadOnlyList<ActionCenterItemDto> ActionCenter,
    IReadOnlyList<AiInsightDto> AiInsights,
    DashboardInventoryDto Inventory);

public sealed record QuickStatsDto(IReadOnlyList<QuickStatItemDto> Items);

public sealed record QuickStatItemDto(
    string Label,
    string Value,
    string? Trend,        // "up" | "down" | "flat"
    string? TrendLabel,
    string Icon,
    string Accent);       // "primary" | "success" | "warning" | "danger"

public sealed record InventoryChartsDto(
    IReadOnlyList<StatusBreakdownDto> StatusBreakdown,
    IReadOnlyList<FuelBreakdownDto> FuelBreakdown,
    IReadOnlyList<AgingBucketDto> AgingHistogram,
    IReadOnlyList<MonthlySalesDto> MonthlySales);

public sealed record StatusBreakdownDto(string Status, int Count);

public sealed record FuelBreakdownDto(string FuelType, int Count);

public sealed record AgingBucketDto(string Bucket, int Count);

public sealed record MonthlySalesDto(string Month, int Sold, decimal Revenue);

public sealed record ActionCenterItemDto(
    Guid Id,
    string Category,      // "pricing" | "aging" | "demand" | "follow-up"
    string Title,
    string Description,
    Guid? VehicleId,
    string Severity,     // "info" | "warning" | "critical"
    int? DemandScore = null,
    int? DaysOnLot = null);

public sealed record AiInsightDto(
    Guid Id,
    string Title,
    string Body,
    decimal Confidence,
    Guid? VehicleId,
    DateTimeOffset CreatedAt);

public sealed record DashboardInventoryDto(
    IReadOnlyList<DashboardVehicleDto> Items,
    int Page,
    int Limit,
    int Total);

public sealed record DashboardVehicleDto(
    Guid Id,
    string Vin,
    string StockNumber,
    string Make,
    string Model,
    int Year,
    string Color,
    int Mileage,
    string FuelType,
    decimal AskingPriceAmount,
    string AskingPriceCurrency,
    string Status,
    DateTimeOffset DateAddedToInventoryUtc,
    int DaysInInventory,
    bool IsAging,
    string AgingSeverity,
    string DemandLevel,
    int DemandScore);

public sealed record AgingStockItemDto(
    Guid Id,
    string Vin,
    string StockNumber,
    string Make,
    string Model,
    int Year,
    int DaysInInventory,
    string AgingSeverity,
    decimal AskingPriceAmount,
    string AskingPriceCurrency,
    int DemandScore,
    string DemandLevel);

public sealed record LowInventoryAlertDto(
    Guid Id,
    string Make,
    string Model,
    int Available,
    int Threshold);
