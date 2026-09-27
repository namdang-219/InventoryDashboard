using FluentAssertions;
using IID.Application.Common.Interfaces;
using IID.Application.Features.Dealerships.EventHandlers;
using IID.Domain.Dealerships;
using IID.Domain.Dealerships.Events;
using Moq;

namespace IID.Application.Tests.Features.Dealerships.EventHandlers;

public class DealershipRealtimeEventHandlerTests
{
    private readonly Mock<IDealershipHubNotifier> _notifier = new();

    private static Dealership D() =>
        Dealership.Create(
            name: "AutoNation Honda",
            code: "AN-HON",
            city: "Dallas",
            state: "TX",
            phone: "555-1212",
            nowUtc: DateTimeOffset.UtcNow);

    [Fact]
    public async Task Handle_DealershipAdded_ForwardsToNotifier()
    {
        var sut = new DealershipRealtimeEventHandler(_notifier.Object);
        var dealership = D();
        var ev = new DealershipAdded(dealership);

        await sut.Handle(ev, CancellationToken.None);

        _notifier.Verify(n => n.DealershipAddedAsync(dealership, It.IsAny<CancellationToken>()), Times.Once);
        _notifier.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_DealershipUpdated_ForwardsToNotifier()
    {
        var sut = new DealershipRealtimeEventHandler(_notifier.Object);
        var dealership = D();
        var ev = new DealershipUpdated(dealership);

        await sut.Handle(ev, CancellationToken.None);

        _notifier.Verify(n => n.DealershipUpdatedAsync(dealership, It.IsAny<CancellationToken>()), Times.Once);
        _notifier.VerifyNoOtherCalls();
    }
}
