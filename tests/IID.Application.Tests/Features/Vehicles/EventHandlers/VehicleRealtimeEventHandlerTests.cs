using FluentAssertions;
using IID.Application.Common.Interfaces;
using IID.Application.Features.Vehicles.EventHandlers;
using IID.Domain.Vehicles;
using IID.Domain.Vehicles.Events;
using Moq;

namespace IID.Application.Tests.Features.Vehicles.EventHandlers;

public class VehicleRealtimeEventHandlerTests
{
    private readonly Mock<IVehicleHubNotifier> _notifier = new();

    private static Vehicle V(Guid? id = null) =>
        Vehicle.ForTesting(id ?? Guid.NewGuid(), make: "Honda", model: "Civic", year: 2023);

    [Fact]
    public async Task Handle_VehicleAdded_ForwardsToNotifier()
    {
        var sut = new VehicleRealtimeEventHandler(_notifier.Object);
        var ev = new VehicleAdded(V(), "Honda", "Civic", VehicleStatus.Available);

        await sut.Handle(ev, CancellationToken.None);

        _notifier.Verify(n => n.VehicleAddedAsync(ev.Vehicle, It.IsAny<CancellationToken>()), Times.Once);
        _notifier.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_VehicleUpdated_ForwardsToNotifier()
    {
        var sut = new VehicleRealtimeEventHandler(_notifier.Object);
        var ev = new VehicleUpdated(V(), "Honda");

        await sut.Handle(ev, CancellationToken.None);

        _notifier.Verify(n => n.VehicleUpdatedAsync(ev.Vehicle, It.IsAny<CancellationToken>()), Times.Once);
        _notifier.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_VehicleSold_ForwardsAsVehicleUpdated()
    {
        var sut = new VehicleRealtimeEventHandler(_notifier.Object);
        var ev = new VehicleSold(V(), "Honda", DateTimeOffset.UtcNow, 10);

        await sut.Handle(ev, CancellationToken.None);

        // Vehicles that just sold are broadcast as an update — clients refresh
        // the row to reflect the new Sold status.
        _notifier.Verify(n => n.VehicleUpdatedAsync(ev.Vehicle, It.IsAny<CancellationToken>()), Times.Once);
        _notifier.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_VehicleRemoved_ForwardsVehicleId()
    {
        var sut = new VehicleRealtimeEventHandler(_notifier.Object);
        var vehicle = V();
        var ev = new VehicleRemoved(vehicle, "Honda");

        await sut.Handle(ev, CancellationToken.None);

        _notifier.Verify(n => n.VehicleRemovedAsync(vehicle.Id, It.IsAny<CancellationToken>()), Times.Once);
        _notifier.VerifyNoOtherCalls();
    }
}
