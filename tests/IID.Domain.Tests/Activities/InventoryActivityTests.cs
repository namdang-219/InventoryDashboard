using FluentAssertions;
using IID.Domain.Activities;

namespace IID.Domain.Tests.Activities;

public class InventoryActivityTests
{
    [Fact]
    public void Create_Should_PopulateAllFields()
    {
        var vehicleId = Guid.NewGuid();
        var make = "Honda";
        var message = "Added vehicle";

        var activity = InventoryActivity.Create(
            ActivityType.VehicleAdded,
            message,
            make,
            vehicleId);

        activity.Id.Should().NotBe(Guid.Empty);
        activity.Type.Should().Be(ActivityType.VehicleAdded);
        activity.Message.Should().Be(message);
        activity.Make.Should().Be(make);
        activity.VehicleId.Should().Be(vehicleId);
        activity.OccurredOnUtc.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Create_Should_AllowOptionalMakeAndVehicleId()
    {
        var activity = InventoryActivity.Create(
            ActivityType.VehicleUpdated,
            "msg",
            make: null,
            vehicleId: null);

        activity.Make.Should().BeNull();
        activity.VehicleId.Should().BeNull();
        activity.Type.Should().Be(ActivityType.VehicleUpdated);
    }

    [Fact]
    public void Create_Should_ProduceUniqueIds()
    {
        var a = InventoryActivity.Create(ActivityType.VehicleAdded, "x");
        var b = InventoryActivity.Create(ActivityType.VehicleAdded, "x");

        a.Id.Should().NotBe(b.Id);
    }

    [Fact]
    public void Create_Should_DefaultOccurredOnUtcToNow()
    {
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);
        var activity = InventoryActivity.Create(ActivityType.VehicleRemoved, "y");
        var after = DateTimeOffset.UtcNow.AddSeconds(1);

        activity.OccurredOnUtc.Should().BeOnOrAfter(before);
        activity.OccurredOnUtc.Should().BeOnOrBefore(after);
    }
}
