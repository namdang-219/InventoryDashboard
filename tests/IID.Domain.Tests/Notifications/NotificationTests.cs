using FluentAssertions;
using IID.Domain.Notifications;

namespace IID.Domain.Tests.Notifications;

public class NotificationTests
{
    [Fact]
    public void Create_Should_PopulateAllFields()
    {
        var vehicleId = Guid.NewGuid();

        var notification = Notification.Create(
            NotificationType.NewVehicle,
            "Title",
            "Message body",
            NotificationSeverity.Info,
            vehicleId);

        notification.Id.Should().NotBe(Guid.Empty);
        notification.Type.Should().Be(NotificationType.NewVehicle);
        notification.Title.Should().Be("Title");
        notification.Message.Should().Be("Message body");
        notification.Severity.Should().Be(NotificationSeverity.Info);
        notification.VehicleId.Should().Be(vehicleId);
        notification.IsRead.Should().BeFalse();
        notification.CreatedAtUtc.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Create_Should_AllowNullVehicleId()
    {
        var notification = Notification.Create(
            NotificationType.AgingAlert,
            "T", "M", NotificationSeverity.Warning);

        notification.VehicleId.Should().BeNull();
    }

    [Fact]
    public void MarkAsRead_Should_SetIsReadTrue()
    {
        var notification = Notification.Create(
            NotificationType.NewVehicle, "T", "M", NotificationSeverity.Info);

        notification.MarkAsRead();

        notification.IsRead.Should().BeTrue();
    }

    [Fact]
    public void MarkAsRead_Should_BeIdempotent()
    {
        var notification = Notification.Create(
            NotificationType.NewVehicle, "T", "M", NotificationSeverity.Info);

        notification.MarkAsRead();
        notification.MarkAsRead();

        notification.IsRead.Should().BeTrue();
    }
}
