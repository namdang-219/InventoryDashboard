using FluentAssertions;
using IID.Application.Common.Interfaces;
using IID.Application.Dashboard.Dtos;
using IID.Application.Features.Dashboard.EventHandlers;
using IID.Domain.Vehicles;
using IID.Domain.Vehicles.Events;
using Moq;

namespace IID.Application.Tests.Features.Dashboard.EventHandlers;

public class DashboardRealtimeEventHandlerTests
{
    private readonly Mock<IDashboardService> _dashboard = new();
    private readonly Mock<IVehicleHubNotifier> _hub = new();

    private DashboardRealtimeEventHandler CreateSut()
    {
        _dashboard.Setup(d => d.GetSummaryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DashboardSummaryDto(
                DateTimeOffset.UtcNow, 0, 0, 0, 0, 0, 0,
                new Dictionary<string, int>(), 0m, 0m, 0,
                new List<MakeDistributionDto>(),
                new List<FuelTypeDistributionDto>(),
                new List<DemandLevelDto>()));
        _dashboard.Setup(d => d.GetAlertsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DashboardAlertDto>());

        return new DashboardRealtimeEventHandler(_dashboard.Object, _hub.Object);
    }

    [Fact]
    public async Task Handle_VehicleAdded_Should_BroadcastDashboardUpdate()
    {
        var sut = CreateSut();
        var ev = new VehicleAdded(Guid.NewGuid(), "Honda", "Civic", VehicleStatus.Available);

        await sut.Handle(ev, CancellationToken.None);

        _dashboard.Verify(d => d.GetSummaryAsync(It.IsAny<CancellationToken>()), Times.Once);
        _dashboard.Verify(d => d.GetAlertsAsync(It.IsAny<CancellationToken>()), Times.Once);
        _hub.Verify(h => h.DashboardSummaryUpdatedAsync(It.IsAny<DashboardSummaryDto>(), It.IsAny<CancellationToken>()), Times.Once);
        _hub.Verify(h => h.DashboardAlertsUpdatedAsync(It.IsAny<IReadOnlyList<DashboardAlertDto>>(), It.IsAny<CancellationToken>()), Times.Once);
        _hub.Verify(h => h.InventoryChangedAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_VehicleSold_Should_BroadcastDashboardUpdate()
    {
        var sut = CreateSut();
        var ev = new VehicleSold(Guid.NewGuid(), "Honda", DateTimeOffset.UtcNow, 10);

        await sut.Handle(ev, CancellationToken.None);

        _hub.Verify(h => h.DashboardSummaryUpdatedAsync(It.IsAny<DashboardSummaryDto>(), It.IsAny<CancellationToken>()), Times.Once);
        _hub.Verify(h => h.InventoryChangedAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_VehicleRemoved_Should_BroadcastDashboardUpdate()
    {
        var sut = CreateSut();
        var ev = new VehicleRemoved(Guid.NewGuid(), "Honda");

        await sut.Handle(ev, CancellationToken.None);

        _hub.Verify(h => h.DashboardSummaryUpdatedAsync(It.IsAny<DashboardSummaryDto>(), It.IsAny<CancellationToken>()), Times.Once);
        _hub.Verify(h => h.InventoryChangedAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_VehicleStatusChanged_Should_BroadcastDashboardUpdate()
    {
        var sut = CreateSut();
        var ev = new VehicleStatusChanged(Guid.NewGuid(), VehicleStatus.Available, VehicleStatus.Pending, "Honda");

        await sut.Handle(ev, CancellationToken.None);

        _hub.Verify(h => h.DashboardSummaryUpdatedAsync(It.IsAny<DashboardSummaryDto>(), It.IsAny<CancellationToken>()), Times.Once);
        _hub.Verify(h => h.InventoryChangedAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_VehicleUpdated_Should_BroadcastDashboardUpdate()
    {
        var sut = CreateSut();
        var ev = new VehicleUpdated(Guid.NewGuid(), "Honda");

        await sut.Handle(ev, CancellationToken.None);

        _hub.Verify(h => h.DashboardSummaryUpdatedAsync(It.IsAny<DashboardSummaryDto>(), It.IsAny<CancellationToken>()), Times.Once);
        _hub.Verify(h => h.InventoryChangedAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
