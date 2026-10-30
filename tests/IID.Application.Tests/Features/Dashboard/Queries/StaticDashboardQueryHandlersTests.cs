using FluentAssertions;
using IID.Application.Common.Interfaces;
using IID.Application.Common.Models;
using IID.Application.Dashboard.Dtos;
using IID.Application.Dashboard.Queries.Dtos;
using IID.Application.Dashboard.Queries.GetDashboardAging;
using IID.Application.Dashboard.Queries.GetDashboardBundle;
using IID.Application.Dashboard.Queries.GetLowInventoryAlerts;
using IID.Application.Features.Dashboard.Services;
using IID.Domain.Dashboard;
using IID.Domain.Vehicles;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace IID.Application.Tests.Features.Dashboard.Queries;

public class StaticDashboardQueryHandlersTests
{
    private readonly Mock<IDashboardSnapshotRepository> _snapshots = new();
    private readonly Mock<IDashboardSnapshotCalculator> _calculator = new();
    private readonly Mock<IVehicleRepository> _vehicles = new();
    private readonly Mock<IClock> _clock = new();

    public StaticDashboardQueryHandlersTests()
    {
        _clock.SetupGet(c => c.UtcNow).Returns(new DateTimeOffset(2026, 10, 29, 12, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task GetDashboardBundleHandler_Should_ServeStaticSnapshotWithoutRecalculating_WhenSnapshotExists()
    {
        var now = _clock.Object.UtcNow;
        var dealerId = Guid.NewGuid();
        var summary = new DashboardSummaryDto(now, 42, 30, 5, 5, 2, 4, new Dictionary<string, int> { ["Critical"] = 4 }, 900_000m, 24_324m, 38, [], [], []);
        var quickStats = new QuickStatsDto([new QuickStatItemDto("Total Inventory", "42", null, null, "inventory_2", "primary")]);
        var charts = new InventoryChartsDto([], [], [], []);

        var snapshot = DashboardSnapshot.Create(
            dealerId,
            DashboardSnapshotSerializer.Serialize(summary),
            DashboardSnapshotSerializer.Serialize(quickStats),
            DashboardSnapshotSerializer.Serialize(charts),
            "[]",
            "[]",
            "[]",
            "[]",
            now);

        _snapshots
            .Setup(s => s.GetByScopeAsync($"dealership:{dealerId:D}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(snapshot);

        _vehicles
            .Setup(v => v.ListAsync(It.Is<VehicleListFilter>(f => f.Page == 2 && f.Limit == 20 && f.DealershipId == dealerId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(([], 42));

        var handler = new GetDashboardBundleHandler(
            _snapshots.Object,
            _calculator.Object,
            _vehicles.Object,
            _clock.Object,
            NullLogger<GetDashboardBundleHandler>.Instance);

        var result = await handler.Handle(new GetDashboardBundleQuery(Page: 2, PageSize: 20, DealershipId: dealerId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Summary.TotalInventory.Should().Be(42);
        result.Value.Inventory.Page.Should().Be(2);
        result.Value.Inventory.Total.Should().Be(42);
        _calculator.Verify(
            c => c.CalculateForScopeAsync(It.IsAny<Guid?>(), It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetDashboardBundleHandler_Should_FallbackToCalculator_WhenSnapshotMissing()
    {
        var now = _clock.Object.UtcNow;
        _snapshots
            .Setup(s => s.GetByScopeAsync("global", It.IsAny<CancellationToken>()))
            .ReturnsAsync((DashboardSnapshot?)null);

        _calculator
            .Setup(c => c.CalculateForScopeAsync(null, now, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DashboardSnapshotPayload(
                null,
                "global",
                new DashboardSummaryDto(now, 7, 7, 0, 0, 0, 0, [], 140_000m, 20_000m, 12, [], [], []),
                new QuickStatsDto([]),
                new InventoryChartsDto([], [], [], []),
                [],
                [],
                [],
                [],
                now));

        _vehicles
            .Setup(v => v.ListAsync(It.IsAny<VehicleListFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(([], 7));

        var handler = new GetDashboardBundleHandler(
            _snapshots.Object,
            _calculator.Object,
            _vehicles.Object,
            _clock.Object,
            NullLogger<GetDashboardBundleHandler>.Instance);

        var result = await handler.Handle(new GetDashboardBundleQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Summary.TotalInventory.Should().Be(7);
        _calculator.Verify(c => c.CalculateForScopeAsync(null, now, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetLowInventoryAlertsHandler_Should_ReturnStaticSnapshotAlerts()
    {
        var alerts = new List<LowInventoryAlertDto>
        {
            new(Guid.NewGuid(), "Porsche", "911", 1, 3)
        };
        var snapshot = DashboardSnapshot.Create(
            null,
            "{}",
            "{}",
            "{}",
            "[]",
            "[]",
            DashboardSnapshotSerializer.Serialize(alerts),
            "[]",
            _clock.Object.UtcNow);

        _snapshots
            .Setup(s => s.GetByScopeAsync("global", It.IsAny<CancellationToken>()))
            .ReturnsAsync(snapshot);

        var handler = new GetLowInventoryAlertsHandler(
            _snapshots.Object,
            _calculator.Object,
            NullLogger<GetLowInventoryAlertsHandler>.Instance);

        var result = await handler.Handle(new GetLowInventoryAlertsQuery(null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(a => a.Make == "Porsche" && a.Model == "911" && a.Available == 1);
        _calculator.Verify(
            c => c.CalculateForScopeAsync(It.IsAny<Guid?>(), It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetDashboardAgingHandler_Should_UseSqlPushedListAgingAsync()
    {
        var now = _clock.Object.UtcNow;
        var agingVehicle = Vehicle.Create(
            Vin.Parse("1HGBH41JXMN109186"),
            "Subaru",
            "Outback",
            2022,
            "Green",
            28000,
            FuelType.Petrol,
            Money.Of(22_000m),
            Money.Of(25_900m),
            VehicleStatus.Available,
            now.AddDays(-120),
            now);

        _vehicles
            .Setup(v => v.ListAgingAsync(60, 1, 20, null, now, It.IsAny<CancellationToken>()))
            .ReturnsAsync(([agingVehicle], 1));

        var handler = new GetDashboardAgingHandler(_vehicles.Object, _clock.Object);
        var result = await handler.Handle(new GetDashboardAgingQuery(MinAgeDays: 60, Page: 1, Limit: 20), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Total.Should().Be(1);
        result.Value.Items.Should().ContainSingle(i => i.Make == "Subaru" && i.DaysInInventory == 120);
    }
}

