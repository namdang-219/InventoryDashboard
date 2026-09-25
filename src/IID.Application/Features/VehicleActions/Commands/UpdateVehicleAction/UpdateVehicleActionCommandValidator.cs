using FluentValidation;

namespace IID.Application.VehicleActions.Commands.UpdateVehicleAction;

public sealed class UpdateVehicleActionCommandValidator : AbstractValidator<UpdateVehicleActionCommand>
{
    public UpdateVehicleActionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Action ID is required.");
        RuleFor(x => x.Notes).MaximumLength(2000).WithMessage("Notes must not exceed 2000 characters.");
    }
}
