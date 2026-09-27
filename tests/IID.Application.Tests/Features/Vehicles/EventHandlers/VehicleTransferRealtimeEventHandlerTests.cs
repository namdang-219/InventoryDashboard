using FluentAssertions;
using IID.Application.Common.Interfaces;
using IID.Application.Features.Vehicles.EventHandlers;
using IID.Domain.Vehicles;
using IID.Domain.Vehicles.Events;
using Moq;

namespace IID.Application.Tests.Features.Vehicles.EventHandlers;

public class VehicleTransferRealtimeEventHandlerTests
{
    private readonly Mock<IVehicleHubNotifier> _notifier = new();

    [Fact]
    public async Task Handle_VehicleTransferred_ForwardsWithSourceAndTargetDealershipIds()
    {
        var vehicle = Vehicle.ForTesting(Guid.NewGuid(), make: "Honda", model: "Civic", year: 2023);
        var oldDealer = Guid.NewGuid();
        var newDealer = Guid.NewGuid();
        var sut = new VehicleTransferRealtimeEventHandler(_notifier.Object);
        var ev = new VehicleTransferred(vehicle, oldDealer, newDealer, "Honda");

        await sut.Handle(ev, CancellationToken.None);

        _notifier.Verify(n => n.VehicleTransferredAsync(vehicle, oldDealer, It.IsAny<CancellationToken>()), Times.Once);
        _notifier.VerifyNoOtherCalls();
    }
}
