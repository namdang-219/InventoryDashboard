using FastEndpoints;
using FluentValidation;
using IID.Domain.Vehicles;

namespace IID.Api.Endpoints.CreateVehicleEndpoint.Validators;

/// <summary>
/// HTTP-shape validation for <see cref="CreateVehicleRequest"/>.
/// Mirrors Sportcast's <c>PriceRequestValidators</c> — only checks the request envelope,
/// not business invariants. Business invariants (VIN uniqueness, year range, status transitions)
/// are enforced by <c>CreateVehicleCommandValidator</c> on the MediatR side.
/// </summary>
public sealed class CreateVehicleRequestValidator : Validator<CreateVehicleRequest>
{
    public CreateVehicleRequestValidator()
    {
        RuleFor(x => x.Vin).NotEmpty().Length(11, 17);
        RuleFor(x => x.Make).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Model).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Color).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Year).InclusiveBetween(1900, DateTime.UtcNow.Year + 1);
        RuleFor(x => x.Mileage).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PurchasePrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.AskingPrice).GreaterThanOrEqualTo(0);

        // Status is supplied as a string over the wire; verify it parses to a known enum value
        // before the handler reaches the command layer.
        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(s => Enum.TryParse<VehicleStatus>(s, ignoreCase: true, out _))
            .WithMessage($"Status must be one of: {string.Join(", ", Enum.GetNames<VehicleStatus>())}.");

        RuleFor(x => x.DateAddedToInventory)
            .NotEqual(default(DateTimeOffset));
    }
}
