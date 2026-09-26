using FluentAssertions;
using IID.Domain.VehicleActions;

namespace IID.Domain.Tests.VehicleActions;

public class VehicleActionTests
{
    [Fact]
    public void Log_Should_CreateWithDefaults()
    {
        var now = DateTimeOffset.UtcNow;
        var action = VehicleAction.Log(
            Guid.NewGuid(), VehicleActionType.Other, "hello", "user-1", now);

        action.Id.Should().NotBe(Guid.Empty);
        action.Notes.Should().Be("hello");
        action.LoggedAt.Should().Be(now);
        action.CreatedAt.Should().Be(now);

        var ev = action.DomainEvents.OfType<IID.Domain.VehicleActions.Events.VehicleActionLogged>().Single();
        ev.ActionId.Should().Be(action.Id);
        ev.VehicleId.Should().Be(action.VehicleId);
        ev.ActionType.Should().Be(action.ActionType);
        ev.LoggedByUserId.Should().Be("user-1");
    }

    [Fact]
    public void Log_Should_TrimNotes()
    {
        var action = VehicleAction.Log(
            Guid.NewGuid(), VehicleActionType.Other, "  hello  ", "user-1", DateTimeOffset.UtcNow);

        action.Notes.Should().Be("hello");
    }

    [Fact]
    public void Log_Should_Throw_WhenVehicleIdEmpty()
    {
        var act = () => VehicleAction.Log(
            Guid.Empty, VehicleActionType.Other, null, "user-1", DateTimeOffset.UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Log_Should_Throw_WhenLoggedByUserIdEmpty()
    {
        var act = () => VehicleAction.Log(
            Guid.NewGuid(), VehicleActionType.Other, null, "", DateTimeOffset.UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Log_Should_Throw_WhenNotesTooLong()
    {
        var longNotes = new string('x', 2001);
        var act = () => VehicleAction.Log(
            Guid.NewGuid(), VehicleActionType.Other, longNotes, "user-1", DateTimeOffset.UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Update_Should_ChangeNotes()
    {
        var action = VehicleAction.Log(
            Guid.NewGuid(), VehicleActionType.Other, "first", "user-1", DateTimeOffset.UtcNow);

        action.Update(VehicleActionType.ManagerReview, "second", DateTimeOffset.UtcNow, "user-2");

        action.ActionType.Should().Be(VehicleActionType.ManagerReview);
        action.Notes.Should().Be("second");
        action.UpdatedByUserId.Should().Be("user-2");
    }

    [Fact]
    public void SoftDelete_Should_SetDeletedAt()
    {
        var action = VehicleAction.Log(
            Guid.NewGuid(), VehicleActionType.Other, null, "user-1", DateTimeOffset.UtcNow);
        var now = DateTimeOffset.UtcNow;
        action.SoftDelete(now, "user-1");

        action.DeletedAt.Should().Be(now);
    }
}
