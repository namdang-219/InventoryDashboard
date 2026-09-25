using FluentValidation;

namespace IID.Application.Vehicles.Commands.UpdateVehicle;

public sealed class UpdateVehicleCommandValidator : AbstractValidator<UpdateVehicleCommand>
{
    public UpdateVehicleCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Vehicle ID is required.");

        RuleFor(x => x.Make)
            .NotEmpty().WithMessage("Make is required.")
            .MaximumLength(50).WithMessage("Make must not exceed 50 characters.");

        RuleFor(x => x.Model)
            .NotEmpty().WithMessage("Model is required.")
            .MaximumLength(50).WithMessage("Model must not exceed 50 characters.");

        RuleFor(x => x.Year)
            .InclusiveBetween(1900, DateTime.UtcNow.Year + 2).WithMessage("Year is out of valid range.");

        RuleFor(x => x.Color)
            .NotEmpty().WithMessage("Color is required.")
            .MaximumLength(30).WithMessage("Color must not exceed 30 characters.");

        RuleFor(x => x.Mileage)
            .GreaterThanOrEqualTo(0).WithMessage("Mileage cannot be negative.");

        RuleFor(x => x.PurchasePrice)
            .GreaterThan(0).WithMessage("Purchase price must be positive.");

        RuleFor(x => x.AskingPrice)
            .GreaterThan(0).WithMessage("Asking price must be positive.");

        RuleFor(x => x.FuelType)
            .IsInEnum().WithMessage("Fuel type is invalid.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Status is invalid.");
    }
}
