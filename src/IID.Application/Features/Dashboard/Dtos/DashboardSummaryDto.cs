namespace IID.Application.Dashboard.Dtos;

public sealed record DashboardSummaryDto(
    DateTimeOffset GeneratedAtUtc,
    int TotalInventory,
    int AvailableCount,
    int PendingCount,
    int SoldCount,
    int WholesaleCount,
    int AgingCount,
    Dictionary<string, int> AgingBySeverity,
    decimal TotalInventoryValue,
    decimal AverageAskingPrice,
    int AverageDaysOnLot,
    List<MakeDistributionDto> TopMakes,
    List<FuelTypeDistributionDto> FuelMix,
    List<DemandLevelDto> DemandDistribution);

public sealed record MakeDistributionDto(string Make, int Count, decimal TotalValue);

public sealed record FuelTypeDistributionDto(string FuelType, int Count);

public sealed record DemandLevelDto(string Level, int Count);
