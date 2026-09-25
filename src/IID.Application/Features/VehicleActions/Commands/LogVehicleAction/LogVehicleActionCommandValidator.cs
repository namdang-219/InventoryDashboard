using FluentValidation;
namespace IID.Application.VehicleActions.Commands.LogVehicleAction;

public sealed class LogVehicleActionCommandValidator : AbstractValidator<LogVehicleActionCommand>
{
    public LogVehicleActionCommandValidator()
    {
        RuleFor(c => c.VehicleId).NotEqual(Guid.Empty);
        RuleFor(c => c.ActionType).IsInEnum();
        RuleFor(c => c.Notes).MaximumLength(2000);
    }
}
