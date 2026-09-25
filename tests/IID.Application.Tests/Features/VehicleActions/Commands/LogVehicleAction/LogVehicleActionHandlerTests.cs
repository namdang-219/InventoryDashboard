using FluentAssertions;
using IID.Application.Common.Interfaces;
using IID.Application.VehicleActions.Commands.LogVehicleAction;
using IID.Domain.Common;
using IID.Domain.Vehicles;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace IID.Application.Tests.Features.VehicleActions.Commands.LogVehicleAction;

public class LogVehicleActionHandlerTests
{
    private readonly Mock<IVehicleRepository> _vehicles = new();
    private readonly Mock<IVehicleActionRepository> _actions = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IVehicleHubNotifier> _notifier = new();
    private readonly Mock<IClock> _clock = new();
    private readonly Mock<ICurrentUser> _user = new();

    private LogVehicleActionHandler CreateSut()
    {
        _clock.SetupGet(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);
        _user.SetupGet(u => u.Id).Returns("user-1");
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(IID.Domain.Common.Result<int>.Success(1));
        return new LogVehicleActionHandler(
            _vehicles.Object, _actions.Object, _uow.Object,
            _notifier.Object, _clock.Object, _user.Object,
            NullLogger<LogVehicleActionHandler>.Instance);
    }

    private static Vehicle CreateVehicle() =>
        Vehicle.Create(
            Vin.Parse("1HGBH41JXMN109186"),
            "Honda", "Civic", 2023, "Red", 0,
            FuelType.Petrol, Money.Of(20000m), Money.Of(25000m),
            VehicleStatus.Available, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

    [Fact]
    public async Task Handle_Should_ReturnSuccess_WhenValid()
    {
        var vehicle = CreateVehicle();
        _vehicles.Setup(v => v.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(vehicle);

        var cmd = new LogVehicleActionCommand(vehicle.Id, Domain.VehicleActions.VehicleActionType.Other, "test notes");
        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
        _actions.Verify(a => a.AddAsync(It.IsAny<Domain.VehicleActions.VehicleAction>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_ReturnUnauthorized_WhenNoUser()
    {
        var vehicle = CreateVehicle();
        _vehicles.Setup(v => v.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(vehicle);
        _user.SetupGet(u => u.Id).Returns((string?)null);

        var sut = new LogVehicleActionHandler(
            _vehicles.Object, _actions.Object, _uow.Object,
            _notifier.Object, _clock.Object, _user.Object,
            NullLogger<LogVehicleActionHandler>.Instance);

        var cmd = new LogVehicleActionCommand(vehicle.Id, Domain.VehicleActions.VehicleActionType.Other, null);
        var result = await sut.Handle(cmd, CancellationToken.None);

        result.ErrorKind.Should().Be(ErrorKind.Unauthorized);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenVehicleMissing()
    {
        _vehicles.Setup(v => v.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Vehicle?)null);

        var cmd = new LogVehicleActionCommand(Guid.NewGuid(), Domain.VehicleActions.VehicleActionType.Other, null);
        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.ErrorKind.Should().Be(ErrorKind.NotFound);
    }

    [Fact]
    public async Task Handle_Should_NotifyHub_OnSuccess()
    {
        var vehicle = CreateVehicle();
        _vehicles.Setup(v => v.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(vehicle);

        await CreateSut().Handle(new LogVehicleActionCommand(vehicle.Id, Domain.VehicleActions.VehicleActionType.Other, null), CancellationToken.None);

        _notifier.Verify(n => n.VehicleActionLoggedAsync(It.IsAny<Domain.VehicleActions.VehicleAction>(), It.IsAny<Vehicle>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
