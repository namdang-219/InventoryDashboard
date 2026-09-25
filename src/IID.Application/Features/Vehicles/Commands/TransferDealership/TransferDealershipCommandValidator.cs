using FluentValidation;

namespace IID.Application.Vehicles.Commands.TransferDealership;

public sealed class TransferDealershipCommandValidator : AbstractValidator<TransferDealershipCommand>
{
    public TransferDealershipCommandValidator()
    {
        RuleFor(x => x.VehicleId).NotEmpty().WithMessage("Vehicle ID is required.");
        RuleFor(x => x.TargetDealershipId).NotEmpty().WithMessage("Target Dealership ID is required.");
    }
}
