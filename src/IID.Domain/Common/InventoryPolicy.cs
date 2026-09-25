namespace IID.Domain.Common;

public static class InventoryPolicy
{
    public const int AgingStockThresholdDays = 90;
    public const int AgingWarningDays = 30;
    public const int AgingHighDays = 60;
    public const int AgingCriticalDays = 90;
    public const int MaxListPageSize = 100;
    public const int DefaultPageSize = 20;
    public const string AgingDashboardGroup = "inventory-dashboard";
    public static string GetDealershipGroup(Guid dealershipId) => $"dealership:{dealershipId}";
}
