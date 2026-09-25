using FluentAssertions;
using IID.Application.Common.Interfaces;
using IID.Application.VehicleActions.Commands.UpdateVehicleAction;
using IID.Domain.Common;
using IID.Domain.VehicleActions;
using Moq;

namespace IID.Application.Tests.Features.VehicleActions.Commands.UpdateVehicleAction;

public class UpdateVehicleActionHandlerTests
{
    private readonly Mock<IVehicleActionRepository> _actions = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IClock> _clock = new();
    private readonly Mock<ICurrentUser> _user = new();

    private UpdateVehicleActionHandler CreateSut()
    {
        _clock.SetupGet(c => c.UtcNow).Returns(DateTimeOffset.UtcNow);
        _user.SetupGet(u => u.Id).Returns("user-1");
        _user.SetupGet(u => u.Email).Returns("saler@iid.local");
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(IID.Domain.Common.Result<int>.Success(1));
        return new UpdateVehicleActionHandler(_actions.Object, _uow.Object, _clock.Object, _user.Object);
    }

    [Fact]
    public async Task Handle_Should_ReturnSuccess_WhenActionFound()
    {
        var action = VehicleAction.Log(Guid.NewGuid(), VehicleActionType.Other, "initial", "user-1", DateTimeOffset.UtcNow);
        _actions.Setup(a => a.GetByIdAsync(action.Id, It.IsAny<CancellationToken>())).ReturnsAsync(action);

        var cmd = new UpdateVehicleActionCommand(action.Id, VehicleActionType.PriceReductionPlanned, "updated notes");
        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        action.ActionType.Should().Be(VehicleActionType.PriceReductionPlanned);
        action.Notes.Should().Be("updated notes");
        action.UpdatedByUserId.Should().Be("saler@iid.local");
        _actions.Verify(a => a.Update(action), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenActionMissing()
    {
        _actions.Setup(a => a.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((VehicleAction?)null);

        var cmd = new UpdateVehicleActionCommand(Guid.NewGuid(), VehicleActionType.Relist, "notes");
        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.ErrorKind.Should().Be(ErrorKind.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnUnauthorized_WhenNoUser()
    {
        _user.SetupGet(u => u.Id).Returns((string?)null);
        var sut = new UpdateVehicleActionHandler(_actions.Object, _uow.Object, _clock.Object, _user.Object);

        var cmd = new UpdateVehicleActionCommand(Guid.NewGuid(), VehicleActionType.Relist, "notes");
        var result = await sut.Handle(cmd, CancellationToken.None);

        result.ErrorKind.Should().Be(ErrorKind.Unauthorized);
    }
}
