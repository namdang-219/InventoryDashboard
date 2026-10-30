using FluentAssertions;
using IID.Application.Common.Interfaces;
using IID.Application.Features.Dashboard.Services;
using IID.Domain.Vehicles;
using Moq;

namespace IID.Application.Tests.Features.Dashboard.Services;

public class DashboardSnapshotCalculatorTests
{
    private readonly Mock<IVehicleRepository> _vehicles = new();
    private readonly Mock<IClock> _clock = new();

    private DashboardSnapshotCalculator CreateSut(DateTimeOffset now)
    {
        _clock.SetupGet(c => c.UtcNow).Returns(now);
        return new DashboardSnapshotCalculator(_vehicles.Object, _clock.Object);
    }

    private static Vehicle CreateVehicle(
        string vin,
        string make,
        string model,
        VehicleStatus status,
        decimal askingPrice,
        int daysOld,
        DateTimeOffset now)
    {
        return Vehicle.Create(
            Vin.Parse(vin),
            make,
            model,
            2023,
            "Silver",
            15000,
            FuelType.Hybrid,
            Money.Of(askingPrice * 0.85m),
            Money.Of(askingPrice),
            status,
            now.AddDays(-daysOld),
            now);
    }

    [Fact]
    public void Calculate_Should_ComputeValuationAndAgingStrictlyOverActiveStock()
    {
        var now = new DateTimeOffset(2026, 10, 29, 12, 0, 0, TimeSpan.Zero);
        var sut = CreateSut(now);

        var activeFresh = CreateVehicle("1HGBH41JXMN109181", "Toyota", "Camry", VehicleStatus.Available, 30_000m, 10, now);
        var activeAging = CreateVehicle("1HGBH41JXMN109182", "Honda", "Accord", VehicleStatus.Pending, 20_000m, 100, now);
        var soldOld = CreateVehicle("1HGBH41JXMN109183", "Ford", "F-150", VehicleStatus.Available, 50_000m, 200, now);
        soldOld.MarkSold(Money.Of(48_000m), now.AddDays(-2), "saler-1");

        var payload = sut.Calculate(null, [activeFresh, activeAging, soldOld], now);

        payload.ScopeKey.Should().Be("global");
        payload.Summary.TotalInventory.Should().Be(3);
        payload.Summary.AvailableCount.Should().Be(1);
        payload.Summary.PendingCount.Should().Be(1);
        payload.Summary.SoldCount.Should().Be(1);

        // Active valuation excludes the 50k Sold vehicle -> 30k + 20k = 50k
        payload.Summary.TotalInventoryValue.Should().Be(50_000m);
        payload.Summary.AverageAskingPrice.Should().Be(25_000m);
        payload.Summary.AgingCount.Should().Be(1); // Only activeAging (100d); soldOld (200d) excluded

        // Monthly sales includes the 48k sold vehicle in current month
        payload.Charts.MonthlySales.Should().HaveCount(12);
        payload.Charts.MonthlySales[^1].Sold.Should().Be(1);
        payload.Charts.MonthlySales[^1].Revenue.Should().Be(48_000m);
    }

    [Fact]
    public void Calculate_Should_GenerateDeterministicLowInventoryAlertsAndRealtimeAlerts()
    {
        var now = new DateTimeOffset(2026, 10, 29, 12, 0, 0, TimeSpan.Zero);
        var sut = CreateSut(now);

        var v1 = CreateVehicle("1HGBH41JXMN109181", "Toyota", "RAV4", VehicleStatus.Available, 32_000m, 95, now);
        var v2 = CreateVehicle("1HGBH41JXMN109182", "BMW", "X5", VehicleStatus.Available, 60_000m, 65, now);
        var v3 = CreateVehicle("1HGBH41JXMN109183", "BMW", "X5", VehicleStatus.Available, 61_000m, 15, now);

        var first = sut.Calculate(null, [v1, v2, v3], now);
        var second = sut.Calculate(null, [v1, v2, v3], now);

        first.LowInventoryAlerts.Should().HaveCount(2);
        first.LowInventoryAlerts[0].Make.Should().Be("Toyota"); // 1 available (more critical than 2 available)
        first.LowInventoryAlerts[0].Id.Should().Be(second.LowInventoryAlerts[0].Id);

        first.RealtimeAlerts.Should().HaveCount(2);
        first.RealtimeAlerts[0].Severity.Should().Be("Critical");
        first.RealtimeAlerts[1].Severity.Should().Be("High");
    }
}

