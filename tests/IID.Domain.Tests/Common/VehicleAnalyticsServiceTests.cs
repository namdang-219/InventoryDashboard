using FluentAssertions;
using IID.Domain.Common;
using IID.Domain.Vehicles;

namespace IID.Domain.Tests.Common;

public class VehicleAnalyticsServiceTests
{
    private static Vehicle CreateAvailable(DateTimeOffset addedAt, DateTimeOffset asOf, FuelType fuel = FuelType.Petrol, int year = 2023)
    {
        return Vehicle.Create(
            Vin.Parse("1HGBH41JXMN109186"),
            "Make", "Model", year, "Color", 0,
            fuel, Money.Of(1000m), Money.Of(2000m),
            VehicleStatus.Available, addedAt, asOf);
    }

    [Fact]
    public void IsAging_Should_BeTrue_WhenOver90Days()
    {
        var now = DateTimeOffset.UtcNow;
        var svc = new VehicleAnalyticsService(now);
        var v = CreateAvailable(now.AddDays(-100), now);

        svc.IsAging(v).Should().BeTrue();
    }

    [Fact]
    public void IsAging_Should_BeFalse_WhenUnder90Days()
    {
        var now = DateTimeOffset.UtcNow;
        var svc = new VehicleAnalyticsService(now);
        var v = CreateAvailable(now.AddDays(-30), now);

        svc.IsAging(v).Should().BeFalse();
    }

    [Fact]
    public void GetAgingSeverity_Should_BucketByDays()
    {
        var now = DateTimeOffset.UtcNow;
        var svc = new VehicleAnalyticsService(now);
        var fresh = CreateAvailable(now.AddDays(-10), now);
        var warning = CreateAvailable(now.AddDays(-45), now);
        var high = CreateAvailable(now.AddDays(-75), now);
        var critical = CreateAvailable(now.AddDays(-120), now);

        svc.GetAgingSeverity(fresh).Should().Be(AgingSeverity.None);
        svc.GetAgingSeverity(warning).Should().Be(AgingSeverity.Warning);
        svc.GetAgingSeverity(high).Should().Be(AgingSeverity.High);
        svc.GetAgingSeverity(critical).Should().Be(AgingSeverity.Critical);
    }

    [Fact]
    public void BucketByAging_Should_CountAllSeverities()
    {
        var now = DateTimeOffset.UtcNow;
        var svc = new VehicleAnalyticsService(now);
        var vehicles = new[]
        {
            CreateAvailable(now.AddDays(-1), now),
            CreateAvailable(now.AddDays(-45), now),
            CreateAvailable(now.AddDays(-75), now),
            CreateAvailable(now.AddDays(-120), now)
        };

        var buckets = svc.BucketByAging(vehicles);

        buckets.Should().HaveCount(4);
        buckets[AgingSeverity.None].Should().Be(1);
        buckets[AgingSeverity.Warning].Should().Be(1);
        buckets[AgingSeverity.High].Should().Be(1);
        buckets[AgingSeverity.Critical].Should().Be(1);
    }

    [Fact]
    public void GetDemandScore_Should_DelegateToVehicle()
    {
        var now = DateTimeOffset.UtcNow;
        var svc = new VehicleAnalyticsService(now);
        var v = CreateAvailable(now.AddDays(-100), now, FuelType.Petrol, 2018);

        svc.GetDemandScore(v).Should().Be(v.GetDemandScore(now));
    }
}
