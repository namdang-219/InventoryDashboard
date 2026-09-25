using FluentValidation;
using IID.Application.Common.Interfaces;

namespace IID.Application.Features.Dealerships.Commands.UpdateDealership;

public sealed class UpdateDealershipCommandValidator : AbstractValidator<UpdateDealershipCommand>
{
    public UpdateDealershipCommandValidator(IDealershipRepository repository)
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Dealership ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Dealership name is required.")
            .MaximumLength(150).WithMessage("Dealership name cannot exceed 150 characters.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Dealership code is required.")
            .MaximumLength(20).WithMessage("Dealership code cannot exceed 20 characters.")
            .MustAsync(async (cmd, code, ct) => !await repository.CodeExistsAsync(code, cmd.Id, ct))
            .WithMessage("Another dealership with this code already exists.");

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("City is required.")
            .MaximumLength(100).WithMessage("City cannot exceed 100 characters.");

        RuleFor(x => x.State)
            .NotEmpty().WithMessage("State is required.")
            .MaximumLength(50).WithMessage("State cannot exceed 50 characters.");

        RuleFor(x => x.Phone)
            .MaximumLength(50).WithMessage("Phone cannot exceed 50 characters.");
    }
}
