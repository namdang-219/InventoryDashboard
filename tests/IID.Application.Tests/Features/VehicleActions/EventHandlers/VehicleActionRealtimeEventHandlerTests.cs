using FluentAssertions;
using IID.Application.Common.Interfaces;
using IID.Application.Features.VehicleActions.EventHandlers;
using IID.Domain.VehicleActions;
using IID.Domain.VehicleActions.Events;
using IID.Domain.Vehicles;
using Moq;

namespace IID.Application.Tests.Features.VehicleActions.EventHandlers;

public class VehicleActionRealtimeEventHandlerTests
{
    private readonly Mock<IVehicleHubNotifier> _notifier = new();

    [Fact]
    public async Task Handle_WhenVehicleAttached_ForwardsActionAndVehicle_AndTriggersInventoryRefresh()
    {
        var vehicle = Vehicle.ForTesting(Guid.NewGuid(), make: "Honda", model: "Civic", year: 2023);
        var action = VehicleAction.ForTesting(Guid.NewGuid(), vehicle.Id);
        var sut = new VehicleActionRealtimeEventHandler(_notifier.Object);
        var ev = new VehicleActionLogged(action, action.ActionType, action.LoggedByUserId) { Vehicle = vehicle };

        await sut.Handle(ev, CancellationToken.None);

        _notifier.Verify(n => n.VehicleActionLoggedAsync(action, vehicle, It.IsAny<CancellationToken>()), Times.Once);
        _notifier.Verify(n => n.InventoryChangedAsync(It.IsAny<CancellationToken>()), Times.Once);
        _notifier.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_WhenVehicleMissing_FallsBackToActionOnlyOverload()
    {
        var action = VehicleAction.ForTesting(Guid.NewGuid(), Guid.NewGuid());
        var sut = new VehicleActionRealtimeEventHandler(_notifier.Object);
        var ev = new VehicleActionLogged(action, action.ActionType, action.LoggedByUserId); // Vehicle=null

        await sut.Handle(ev, CancellationToken.None);

        _notifier.Verify(n => n.VehicleActionLoggedAsync(action, It.IsAny<CancellationToken>()), Times.Once);
        _notifier.Verify(n => n.InventoryChangedAsync(It.IsAny<CancellationToken>()), Times.Once);
        _notifier.VerifyNoOtherCalls();
    }
}
