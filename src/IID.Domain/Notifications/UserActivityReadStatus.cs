using IID.Domain.Common;

namespace IID.Domain.Notifications;

/// <summary>
/// Tracks whether a user account has read an activity feed notification.
/// </summary>
public sealed class UserActivityReadStatus : Entity
{
    public string UserId { get; private set; } = string.Empty;
    public string ActivityId { get; private set; } = string.Empty;
    public bool IsRead { get; private set; } = true;
    public DateTimeOffset ReadAtUtc { get; private set; } = DateTimeOffset.UtcNow;

    private UserActivityReadStatus() { }

    public static UserActivityReadStatus Create(string userId, string activityId, DateTimeOffset? readAtUtc = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId, nameof(userId));
        ArgumentException.ThrowIfNullOrWhiteSpace(activityId, nameof(activityId));

        return new UserActivityReadStatus
        {
            Id = Guid.NewGuid(),
            UserId = userId.Trim(),
            ActivityId = activityId.Trim(),
            IsRead = true,
            ReadAtUtc = readAtUtc ?? DateTimeOffset.UtcNow
        };
    }

    public void MarkAsRead(DateTimeOffset readAtUtc)
    {
        IsRead = true;
        ReadAtUtc = readAtUtc;
    }
}
