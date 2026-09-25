using FluentValidation;
using IID.Application.Common.Interfaces;

namespace IID.Application.Features.Dealerships.Commands.CreateDealership;

public sealed class CreateDealershipCommandValidator : AbstractValidator<CreateDealershipCommand>
{
    public CreateDealershipCommandValidator(IDealershipRepository repository)
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Dealership name is required.")
            .MaximumLength(150).WithMessage("Dealership name cannot exceed 150 characters.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Dealership code is required.")
            .MaximumLength(20).WithMessage("Dealership code cannot exceed 20 characters.")
            .MustAsync(async (code, ct) => !await repository.CodeExistsAsync(code, null, ct))
            .WithMessage("A dealership with this code already exists.");

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
