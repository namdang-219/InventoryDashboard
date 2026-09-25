using FluentAssertions;
using IID.Application.Common.Interfaces;
using IID.Application.Features.Dashboard.Services;
using IID.Domain.Vehicles;
using Moq;

namespace IID.Application.Tests.Features.Dashboard.Services;

public class DashboardServiceTests
{
    private readonly Mock<IVehicleRepository> _repo = new();
    private readonly Mock<IClock> _clock = new();

    private DashboardService CreateSut()
    {
        _clock.SetupGet(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);
        return new DashboardService(_repo.Object, _clock.Object);
    }

    private static Vehicle MakeVehicle(VehicleStatus status, int daysOld, DateTimeOffset now)
    {
        return Vehicle.Create(
            Vin.Parse("1HGBH41JXMN109186"),
            "Make", "Model", 2023, "Red", 0,
            FuelType.Petrol, Money.Of(1000m), Money.Of(2000m),
            status, now.AddDays(-daysOld), now);
    }

    [Fact]
    public async Task GetSummaryAsync_Should_ReturnEmpty_WhenNoVehicles()
    {
        _repo.Setup(r => r.ListAsync(
                null, null, null, null, null,
                1, 1000, "dateAdded", "desc",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Vehicle>(), 0));

        var result = await CreateSut().GetSummaryAsync(CancellationToken.None);

        result.TotalInventory.Should().Be(0);
        result.AvailableCount.Should().Be(0);
        result.AgingCount.Should().Be(0);
        result.TotalInventoryValue.Should().Be(0m);
    }

    [Fact]
    public async Task GetSummaryAsync_Should_CountStatusBreakdown()
    {
        var now = DateTimeOffset.UtcNow;
        var vehicles = new List<Vehicle>
        {
            MakeVehicle(VehicleStatus.Available, 10, now),
            MakeVehicle(VehicleStatus.Available, 20, now),
            MakeVehicle(VehicleStatus.Pending, 15, now),
            MakeVehicle(VehicleStatus.Sold, 5, now),
            MakeVehicle(VehicleStatus.Wholesale, 30, now),
        };
        _repo.Setup(r => r.ListAsync(
                null, null, null, null, null,
                1, 1000, "dateAdded", "desc",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((vehicles, vehicles.Count));

        var result = await CreateSut().GetSummaryAsync(CancellationToken.None);

        result.TotalInventory.Should().Be(5);
        result.AvailableCount.Should().Be(2);
        result.PendingCount.Should().Be(1);
        result.SoldCount.Should().Be(1);
        result.WholesaleCount.Should().Be(1);
    }

    [Fact]
    public async Task GetSummaryAsync_Should_SumAskingPrice_ForTotalValue()
    {
        var now = DateTimeOffset.UtcNow;
        var v1 = Vehicle.Create(
            Vin.Parse("1HGBH41JXMN109186"),
            "A", "Model", 2023, "Red", 0,
            FuelType.Petrol, Money.Of(1m), Money.Of(1000m),
            VehicleStatus.Available, now, now);
        var v2 = Vehicle.Create(
            Vin.Parse("JH4DA1750HS000001"),
            "B", "Model", 2023, "Red", 0,
            FuelType.Petrol, Money.Of(1m), Money.Of(2500m),
            VehicleStatus.Available, now, now);
        _repo.Setup(r => r.ListAsync(
                null, null, null, null, null,
                1, 1000, "dateAdded", "desc",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Vehicle> { v1, v2 }, 2));

        var result = await CreateSut().GetSummaryAsync(CancellationToken.None);

        result.TotalInventoryValue.Should().Be(3500m);
    }

    [Fact]
    public async Task GetAlertsAsync_Should_ReturnEmpty_WhenNoAging()
    {
        var now = DateTimeOffset.UtcNow;
        var vehicles = new List<Vehicle>
        {
            MakeVehicle(VehicleStatus.Available, 10, now),
            MakeVehicle(VehicleStatus.Available, 20, now),
        };
        _repo.Setup(r => r.ListAsync(
                null, null, null, null, null,
                1, 1000, "dateAdded", "desc",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((vehicles, vehicles.Count));

        var alerts = await CreateSut().GetAlertsAsync(CancellationToken.None);

        alerts.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAlertsAsync_Should_FlagAvailableVehiclesAboveWarning()
    {
        var now = DateTimeOffset.UtcNow;
        var vehicles = new List<Vehicle>
        {
            MakeVehicle(VehicleStatus.Available, 35, now),   // Warning
            MakeVehicle(VehicleStatus.Available, 65, now),   // High
            MakeVehicle(VehicleStatus.Available, 95, now),   // Critical
            MakeVehicle(VehicleStatus.Sold, 95, now),         // Sold: skipped
        };
        _repo.Setup(r => r.ListAsync(
                null, null, null, null, null,
                1, 1000, "dateAdded", "desc",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((vehicles, vehicles.Count));

        var alerts = await CreateSut().GetAlertsAsync(CancellationToken.None);

        alerts.Should().HaveCount(3);
        alerts.Should().OnlyContain(a => a.VehicleId != null);
        alerts.Should().Contain(a => a.Severity == "Warning");
        alerts.Should().Contain(a => a.Severity == "High");
        alerts.Should().Contain(a => a.Severity == "Critical");
    }

    [Fact]
    public async Task GetAlertsAsync_Should_CapResultsAt10()
    {
        var now = DateTimeOffset.UtcNow;
        var vehicles = Enumerable.Range(0, 20)
            .Select(_ => MakeVehicle(VehicleStatus.Available, 95, now))
            .ToList();
        _repo.Setup(r => r.ListAsync(
                null, null, null, null, null,
                1, 1000, "dateAdded", "desc",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((vehicles, vehicles.Count));

        var alerts = await CreateSut().GetAlertsAsync(CancellationToken.None);

        alerts.Should().HaveCount(10);
    }

    [Fact]
    public async Task GetAlertsAsync_Should_IncludeCriticalMessage()
    {
        var now = DateTimeOffset.UtcNow;
        var v = MakeVehicle(VehicleStatus.Available, 100, now);
        _repo.Setup(r => r.ListAsync(
                null, null, null, null, null,
                1, 1000, "dateAdded", "desc",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Vehicle> { v }, 1));

        var alerts = await CreateSut().GetAlertsAsync(CancellationToken.None);

        alerts.Should().HaveCount(1);
        alerts[0].Message.Should().Contain("CRITICAL");
        alerts[0].Severity.Should().Be("Critical");
    }
}
