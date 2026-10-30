using FluentAssertions;
using IID.Application.Common.Messaging;
using IID.Application.Features.Dashboard.EventHandlers;
using IID.Domain.Dealerships;
using IID.Domain.Dealerships.Events;
using IID.Domain.Vehicles;
using IID.Domain.Vehicles.Events;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace IID.Application.Tests.Features.Dashboard.EventHandlers;

public class VehicleMutationKafkaEventHandlerTests
{
    private readonly Mock<IKafkaEventProducer> _producer = new();

    private VehicleMutationKafkaEventHandler CreateSut() =>
        new(_producer.Object, NullLogger<VehicleMutationKafkaEventHandler>.Instance);

    private static Vehicle MakeVehicle(Guid dealershipId) =>
        Vehicle.Create(
            Vin.Parse("1HGBH41JXMN109186"),
            "Honda",
            "Civic",
            2024,
            "Blue",
            12000,
            FuelType.Petrol,
            Money.Of(20000m),
            Money.Of(24000m),
            VehicleStatus.Available,
            DateTimeOffset.UtcNow.AddDays(-5),
            DateTimeOffset.UtcNow,
            dealershipId: dealershipId);

    [Fact]
    public async Task Handle_VehicleAdded_Should_PublishAddedEventToKafka()
    {
        var dealerId = Guid.NewGuid();
        var vehicle = MakeVehicle(dealerId);
        var sut = CreateSut();

        await sut.Handle(new VehicleAdded(vehicle, "Honda", "Civic", VehicleStatus.Available), CancellationToken.None);

        _producer.Verify(p => p.PublishAsync(
            KafkaTopics.VehicleMutations,
            dealerId.ToString("D"),
            It.Is<VehicleMutatedIntegrationEvent>(e =>
                e.MutationType == VehicleMutationTypes.Added &&
                e.VehicleId == vehicle.Id &&
                e.DealershipId == dealerId &&
                e.PreviousDealershipId == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_VehicleTransferred_Should_IncludeBothDealershipIds()
    {
        var oldDealerId = Guid.NewGuid();
        var newDealerId = Guid.NewGuid();
        var vehicle = MakeVehicle(newDealerId);
        var sut = CreateSut();

        await sut.Handle(
            new VehicleTransferred(vehicle, oldDealerId, newDealerId, "Honda"),
            CancellationToken.None);

        _producer.Verify(p => p.PublishAsync(
            KafkaTopics.VehicleMutations,
            newDealerId.ToString("D"),
            It.Is<VehicleMutatedIntegrationEvent>(e =>
                e.MutationType == VehicleMutationTypes.Transferred &&
                e.VehicleId == vehicle.Id &&
                e.DealershipId == newDealerId &&
                e.PreviousDealershipId == oldDealerId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_VehicleLifecycleEvents_Should_PublishMatchingMutationTypes()
    {
        var dealerId = Guid.NewGuid();
        var vehicle = MakeVehicle(dealerId);
        var sut = CreateSut();

        await sut.Handle(new VehicleUpdated(vehicle, "Honda"), CancellationToken.None);
        await sut.Handle(new VehicleStatusChanged(vehicle, VehicleStatus.Available, VehicleStatus.Pending, "Honda"), CancellationToken.None);
        await sut.Handle(new VehicleSold(vehicle, "Honda", DateTimeOffset.UtcNow, 12), CancellationToken.None);
        await sut.Handle(new VehicleRemoved(vehicle, "Honda"), CancellationToken.None);

        _producer.Verify(p => p.PublishAsync(
            KafkaTopics.VehicleMutations,
            dealerId.ToString("D"),
            It.Is<VehicleMutatedIntegrationEvent>(e => e.MutationType == VehicleMutationTypes.Updated),
            It.IsAny<CancellationToken>()), Times.Once);
        _producer.Verify(p => p.PublishAsync(
            KafkaTopics.VehicleMutations,
            dealerId.ToString("D"),
            It.Is<VehicleMutatedIntegrationEvent>(e => e.MutationType == VehicleMutationTypes.StatusChanged),
            It.IsAny<CancellationToken>()), Times.Once);
        _producer.Verify(p => p.PublishAsync(
            KafkaTopics.VehicleMutations,
            dealerId.ToString("D"),
            It.Is<VehicleMutatedIntegrationEvent>(e => e.MutationType == VehicleMutationTypes.Sold),
            It.IsAny<CancellationToken>()), Times.Once);
        _producer.Verify(p => p.PublishAsync(
            KafkaTopics.VehicleMutations,
            dealerId.ToString("D"),
            It.Is<VehicleMutatedIntegrationEvent>(e => e.MutationType == VehicleMutationTypes.Removed),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DealershipAdded_Should_PublishDealershipCreatedMutation()
    {
        var dealer = Dealership.Create("Austin North", "DLR-AUS", "Austin", "TX", "555-0100", DateTimeOffset.UtcNow);
        var sut = CreateSut();

        await sut.Handle(new DealershipAdded(dealer), CancellationToken.None);

        _producer.Verify(p => p.PublishAsync(
            KafkaTopics.VehicleMutations,
            dealer.Id.ToString("D"),
            It.Is<VehicleMutatedIntegrationEvent>(e =>
                e.MutationType == VehicleMutationTypes.DealershipCreated &&
                e.DealershipId == dealer.Id &&
                e.VehicleId == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_NotThrow_WhenKafkaBrokerFailsPostCommit()
    {
        _producer
            .Setup(p => p.PublishAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<VehicleMutatedIntegrationEvent>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Broker unreachable"));

        var vehicle = MakeVehicle(Guid.NewGuid());
        var sut = CreateSut();

        var act = () => sut.Handle(new VehicleUpdated(vehicle, "Honda"), CancellationToken.None);
        await act.Should().NotThrowAsync();
    }
}

