using FluentAssertions;
using IID.Application.Common.Interfaces;
using IID.Application.Features.Activities.Queries.GetActivities;
using IID.Domain.VehicleActions;
using IID.Domain.Vehicles;
using Moq;

namespace IID.Application.Tests.Features.Activities;

public class GetActivitiesHandlerTests
{
    private readonly Mock<IVehicleActionRepository> _vehicleActionRepositoryMock = new();
    private readonly Mock<IVehicleRepository> _vehicleRepositoryMock = new();
    private readonly Mock<IUserActivityReadRepository> _userActivityReadRepositoryMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();

    [Fact]
    public async Task Handle_Should_ReturnActivitiesWithReadStatusAndVehicleNames()
    {
        // Arrange
        var userId = "user-123";
        _currentUserMock.Setup(u => u.Id).Returns(userId);

        var actionId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var action = VehicleAction.Log(
            vehicleId,
            VehicleActionType.Other,
            "Price adjusted",
            userId,
            DateTimeOffset.UtcNow);

        _vehicleActionRepositoryMock.Setup(a => a.ListByCursorAsync(null, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<VehicleAction> { action }, "cursor-next", false, 1));

        var vehicle = Vehicle.Create(
            Vin.Parse("1HGBH41JXMN109186"),
            "Toyota",
            "Camry",
            2024,
            "White",
            1000,
            FuelType.Petrol,
            Money.Of(25000m),
            Money.Of(28000m),
            VehicleStatus.Available,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

        _vehicleRepositoryMock.Setup(v => v.GetByIdAsync(vehicleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vehicle);

        _userActivityReadRepositoryMock.Setup(r => r.GetReadActivityIdsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<string> { action.Id.ToString() });

        _userActivityReadRepositoryMock.Setup(r => r.GetUnreadCountAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var handler = new GetActivitiesHandler(
            _vehicleActionRepositoryMock.Object,
            _vehicleRepositoryMock.Object,
            _userActivityReadRepositoryMock.Object,
            _currentUserMock.Object);

        // Act
        var result = await handler.Handle(new GetActivitiesQuery(10, null), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Items.Should().HaveCount(1);
        result.Value.Items[0].IsRead.Should().BeTrue();
        result.Value.Items[0].VehicleName.Should().Be("2024 Toyota Camry");
        result.Value.Total.Should().Be(1);
    }
}
