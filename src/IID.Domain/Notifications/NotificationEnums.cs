namespace IID.Domain.Notifications;

public enum NotificationType
{
    NewVehicle,
    VehicleUpdated,
    VehicleSold,
    VehicleRemoved,
    AgingAlert
}

public enum NotificationSeverity
{
    Info,
    Warning,
    Success,
    Error
}
