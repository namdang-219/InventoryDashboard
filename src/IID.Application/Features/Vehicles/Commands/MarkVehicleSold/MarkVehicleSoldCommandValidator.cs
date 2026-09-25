using FluentValidation;

namespace IID.Application.Vehicles.Commands.MarkVehicleSold;

public sealed class MarkVehicleSoldCommandValidator : AbstractValidator<MarkVehicleSoldCommand>
{
    public MarkVehicleSoldCommandValidator()
    {
        RuleFor(c => c.VehicleId).NotEmpty();
        RuleFor(c => c.SoldPrice).GreaterThanOrEqualTo(0);
        RuleFor(c => c.SoldPriceCurrency).Length(3).When(c => !string.IsNullOrWhiteSpace(c.SoldPriceCurrency));

        // Optimistic concurrency: at least one of the two rowVersion shapes must be present.
        RuleFor(c => c)
            .Must(c => !string.IsNullOrWhiteSpace(c.RowVersionBase64) || (c.RowVersion is { Length: > 0 }))
            .WithMessage("rowVersion (Base64) is required for optimistic concurrency.");
    }
}
