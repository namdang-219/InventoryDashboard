using FastEndpoints;
using FluentValidation;
using IID.Domain.VehicleActions;

namespace IID.Api.Endpoints.LogVehicleActionEndpoint.Validators;

/// <summary>
/// HTTP-shape validation for <see cref="LogVehicleActionRequest"/>.
/// Mirrors Sportcast's <c>PriceRequestValidators</c> — request envelope only.
/// Business invariants (vehicle exists, action type allowed in current state)
/// are enforced by <c>LogVehicleActionCommandValidator</c> on the MediatR side.
/// </summary>
public sealed class LogVehicleActionRequestValidator : Validator<LogVehicleActionRequest>
{
    public LogVehicleActionRequestValidator()
    {
        RuleFor(x => x.ActionType)
            .NotEmpty()
            .Must(s => Enum.TryParse<VehicleActionType>(s, ignoreCase: true, out _))
            .WithMessage($"actionType must be one of: {string.Join(", ", Enum.GetNames<VehicleActionType>())}.");

        RuleFor(x => x.Notes).MaximumLength(2_000);
    }
}
