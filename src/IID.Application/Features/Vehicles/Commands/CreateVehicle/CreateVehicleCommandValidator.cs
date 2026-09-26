using FluentValidation;
namespace IID.Application.Vehicles.Commands.CreateVehicle;

public sealed class CreateVehicleCommandValidator : AbstractValidator<CreateVehicleCommand>
{
    public CreateVehicleCommandValidator()
    {
        RuleFor(c => c.Vin).NotEmpty().Matches("^[A-HJ-NPR-Z0-9]{17}$")
            .WithMessage("VIN must be 17 chars, charset [A-HJ-NPR-Z0-9].");
        RuleFor(c => c.Make).NotEmpty().MaximumLength(50);
        RuleFor(c => c.Model).NotEmpty().MaximumLength(50);
        RuleFor(c => c.Year).InclusiveBetween(1980, DateTimeOffset.UtcNow.Year + 1);
        RuleFor(c => c.Color).NotEmpty().MaximumLength(30);
        RuleFor(c => c.Mileage).GreaterThanOrEqualTo(0);
        RuleFor(c => c.PurchasePrice).GreaterThanOrEqualTo(0);
        RuleFor(c => c.AskingPrice).GreaterThanOrEqualTo(0);
        RuleFor(c => c.StockNumber)
            .MaximumLength(32).When(c => !string.IsNullOrWhiteSpace(c.StockNumber));
        RuleFor(c => c.FuelType).IsInEnum();
        RuleFor(c => c.Status).IsInEnum();
        RuleFor(c => c.DateAddedToInventory)
            .LessThanOrEqualTo(DateTimeOffset.UtcNow.AddSeconds(5))
            .WithMessage("DateAddedToInventory cannot be in the future.");
    }
}
