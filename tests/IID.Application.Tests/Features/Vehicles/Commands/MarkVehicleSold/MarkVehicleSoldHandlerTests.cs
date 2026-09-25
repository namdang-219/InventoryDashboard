using FluentAssertions;
using IID.Application.Common.Interfaces;
using IID.Application.Vehicles.Commands.MarkVehicleSold;
using IID.Domain.Common;
using IID.Domain.Vehicles;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace IID.Application.Tests.Features.Vehicles.Commands.MarkVehicleSold;

public class MarkVehicleSoldHandlerTests
{
    private readonly Mock<IVehicleRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IVehicleHubNotifier> _notifier = new();
    private readonly Mock<IClock> _clock = new();
    private readonly Mock<ICurrentUser> _user = new();

    private static Vehicle CreateAvailable()
    {
        return Vehicle.Create(
            Vin.Parse("1HGBH41JXMN109186"),
            "Honda", "Civic", 2023, "Red", 1000,
            FuelType.Petrol, Money.Of(20000m), Money.Of(25000m),
            VehicleStatus.Available, DateTimeOffset.UtcNow.AddDays(-10), DateTimeOffset.UtcNow);
    }

    private MarkVehicleSoldHandler CreateSut(IVehicleRepository? repo = null)
    {
        _clock.SetupGet(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);
        _user.SetupGet(u => u.Id).Returns("user-1");
        // Default: SaveChangesAsync succeeds with 1 row affected.
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.Success(1));
        return new MarkVehicleSoldHandler(
            repo ?? _repo.Object, _uow.Object, _notifier.Object,
            _clock.Object, _user.Object,
            NullLogger<MarkVehicleSoldHandler>.Instance);
    }

    [Fact]
    public async Task Handle_Should_ReturnSuccess_WhenValid()
    {
        var vehicle = CreateAvailable();
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(vehicle);

        var cmd = new MarkVehicleSoldCommand(vehicle.Id, 26000m);
        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(vehicle.Id);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenVehicleMissing()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Vehicle?)null);

        var cmd = new MarkVehicleSoldCommand(Guid.NewGuid(), 26000m);
        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorKind.Should().Be(ErrorKind.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnConflict_WhenAlreadySold()
    {
        var vehicle = CreateAvailable();
        vehicle.MarkSold(Money.Of(25000m), DateTimeOffset.UtcNow, "user-1");
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(vehicle);

        var cmd = new MarkVehicleSoldCommand(vehicle.Id, 26000m);
        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.ErrorKind.Should().Be(ErrorKind.Conflict);
    }

    [Fact]
    public async Task Handle_Should_NotifyHub_OnSuccess()
    {
        var vehicle = CreateAvailable();
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(vehicle);

        await CreateSut().Handle(new MarkVehicleSoldCommand(vehicle.Id, 26000m), CancellationToken.None);

        _notifier.Verify(n => n.VehicleUpdatedAsync(It.IsAny<Vehicle>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_ApplyRowVersion_OnTrackedEntity()
    {
        var vehicle = CreateAvailable();
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(vehicle);
        var originalRowVersion = vehicle.RowVersion;

        var clientVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        var cmd = new MarkVehicleSoldCommand(vehicle.Id, 26000m, RowVersion: clientVersion);

        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        vehicle.RowVersion.Should().Equal(clientVersion).And.NotEqual(originalRowVersion);
    }

    [Fact]
    public async Task Handle_Should_AcceptBase64RowVersion_FromApi()
    {
        var vehicle = CreateAvailable();
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(vehicle);
        var clientVersion = new byte[] { 9, 9, 9, 9, 9, 9, 9, 9 };

        var cmd = new MarkVehicleSoldCommand(
            vehicle.Id, 26000m,
            RowVersionBase64: Convert.ToBase64String(clientVersion));

        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        vehicle.RowVersion.Should().Equal(clientVersion);
    }

    [Fact]
    public async Task Handle_Should_ReturnValidationFailed_WhenRowVersionBase64IsMalformed()
    {
        var vehicle = CreateAvailable();
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(vehicle);

        var cmd = new MarkVehicleSoldCommand(
            vehicle.Id, 26000m, RowVersionBase64: "not-valid-base64!!!");

        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorKind.Should().Be(ErrorKind.ValidationFailed);
    }

    [Fact]
    public async Task Handle_Should_ReturnConflict_WhenUnitOfWorkReportsConcurrencyFailure()
    {
        var vehicle = CreateAvailable();
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(vehicle);

        var sut = CreateSut();
        // Override the default Success setup *after* CreateSut so this one wins.
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.Failure(ErrorKind.Conflict, "row modified"));

        var cmd = new MarkVehicleSoldCommand(vehicle.Id, 26000m);
        var result = await sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorKind.Should().Be(ErrorKind.Conflict);
        _notifier.Verify(n => n.VehicleUpdatedAsync(It.IsAny<Vehicle>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
