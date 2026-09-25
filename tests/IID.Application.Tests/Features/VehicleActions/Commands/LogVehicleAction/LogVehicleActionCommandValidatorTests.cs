using FluentAssertions;
using FluentValidation.TestHelper;
using IID.Application.VehicleActions.Commands.LogVehicleAction;
using IID.Domain.VehicleActions;

namespace IID.Application.Tests.Features.VehicleActions.Commands.LogVehicleAction;

public class LogVehicleActionCommandValidatorTests
{
    private readonly LogVehicleActionCommandValidator _validator = new();

    [Fact]
    public void Validate_Should_Pass_WhenValid()
    {
        var cmd = new LogVehicleActionCommand(Guid.NewGuid(), VehicleActionType.Other, "notes");

        _validator.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Fail_WhenVehicleIdEmpty()
    {
        var cmd = new LogVehicleActionCommand(Guid.Empty, VehicleActionType.Other, null);

        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(c => c.VehicleId);
    }

    [Fact]
    public void Validate_Should_Fail_WhenNotesTooLong()
    {
        var longNotes = new string('x', 2001);
        var cmd = new LogVehicleActionCommand(Guid.NewGuid(), VehicleActionType.Other, longNotes);

        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(c => c.Notes);
    }

    [Fact]
    public void Validate_Should_Pass_WhenNotesAtBoundary()
    {
        var notes = new string('x', 2000);
        var cmd = new LogVehicleActionCommand(Guid.NewGuid(), VehicleActionType.Other, notes);

        _validator.TestValidate(cmd).ShouldNotHaveValidationErrorFor(c => c.Notes);
    }
}
