using IID.Domain.Common;

namespace IID.Domain.Notifications;

public sealed class Notification : Entity
{
    public NotificationType Type { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public NotificationSeverity Severity { get; private set; }
    public Guid? VehicleId { get; private set; }
    public bool IsRead { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    private Notification() { }

    public static Notification Create(
        NotificationType type,
        string title,
        string message,
        NotificationSeverity severity,
        Guid? vehicleId = null)
    {
        return new Notification
        {
            Id = Guid.NewGuid(),
            Type = type,
            Title = title,
            Message = message,
            Severity = severity,
            VehicleId = vehicleId,
            IsRead = false,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
    }

    public void MarkAsRead()
    {
        IsRead = true;
    }
}
