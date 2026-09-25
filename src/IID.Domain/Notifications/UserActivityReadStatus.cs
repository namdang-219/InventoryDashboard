using System;

namespace IID.Domain.Notifications;

/// <summary>
/// Tracks whether a user account has read an activity feed notification.
/// </summary>
public sealed class UserActivityReadStatus
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string ActivityId { get; set; } = string.Empty;
    public bool IsRead { get; set; } = true;
    public DateTimeOffset ReadAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public static UserActivityReadStatus Create(string userId, string activityId)
    {
        return new UserActivityReadStatus
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ActivityId = activityId,
            IsRead = true,
            ReadAtUtc = DateTimeOffset.UtcNow
        };
    }
}
