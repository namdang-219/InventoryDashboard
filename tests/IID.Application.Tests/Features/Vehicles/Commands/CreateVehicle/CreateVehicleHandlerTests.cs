using FluentAssertions;
using IID.Application.Common.Interfaces;
using IID.Application.Vehicles.Commands.CreateVehicle;
using IID.Domain.Common;
using IID.Domain.Vehicles;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace IID.Application.Tests.Features.Vehicles.Commands.CreateVehicle;

public class CreateVehicleHandlerTests
{
    private readonly Mock<IVehicleRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IVehicleHubNotifier> _notifier = new();
    private readonly Mock<IClock> _clock = new();
    private readonly Mock<ICurrentUser> _user = new();

    private CreateVehicleHandler CreateSut()
    {
        _clock.SetupGet(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);
        _user.SetupGet(u => u.Id).Returns("user-1");
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.Success(1));
        return new CreateVehicleHandler(
            _repo.Object, _uow.Object, _notifier.Object,
            _clock.Object, _user.Object,
            NullLogger<CreateVehicleHandler>.Instance);
    }

    [Fact]
    public async Task Handle_Should_ReturnSuccess_WhenValid()
    {
        _repo.Setup(r => r.VinExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var cmd = new CreateVehicleCommand(
            "1HGBH41JXMN109186", "Honda", "Civic", 2023, "Red", 1000,
            FuelType.Petrol, 15000m, 18000m, VehicleStatus.Available,
            DateTimeOffset.UtcNow.AddDays(-1));
        var sut = CreateSut();

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
        _repo.Verify(r => r.AddAsync(It.IsAny<Vehicle>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_ReturnConflict_WhenVinExists()
    {
        _repo.Setup(r => r.VinExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var cmd = new CreateVehicleCommand(
            "1HGBH41JXMN109186", "Honda", "Civic", 2023, "Red", 1000,
            FuelType.Petrol, 15000m, 18000m, VehicleStatus.Available,
            DateTimeOffset.UtcNow.AddDays(-1));

        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorKind.Should().Be(ErrorKind.Conflict);
        _repo.Verify(r => r.AddAsync(It.IsAny<Vehicle>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnConflict_WhenStockNumberExists()
    {
        _repo.Setup(r => r.VinExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _repo.Setup(r => r.StockNumberExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var cmd = new CreateVehicleCommand(
            "1HGBH41JXMN109186", "Honda", "Civic", 2023, "Red", 1000,
            FuelType.Petrol, 15000m, 18000m, VehicleStatus.Available,
            DateTimeOffset.UtcNow.AddDays(-1), StockNumber: "STK-XYZ");

        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.ErrorKind.Should().Be(ErrorKind.Conflict);
    }

    [Fact]
    public async Task Handle_Should_NotifyHub_OnSuccess()
    {
        _repo.Setup(r => r.VinExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var cmd = new CreateVehicleCommand(
            "1HGBH41JXMN109186", "Honda", "Civic", 2023, "Red", 1000,
            FuelType.Petrol, 15000m, 18000m, VehicleStatus.Available,
            DateTimeOffset.UtcNow.AddDays(-1));

        await CreateSut().Handle(cmd, CancellationToken.None);

        _notifier.Verify(n => n.VehicleAddedAsync(It.IsAny<Vehicle>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
