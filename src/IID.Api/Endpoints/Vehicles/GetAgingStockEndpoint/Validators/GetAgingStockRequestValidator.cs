using FastEndpoints;
using FluentValidation;

namespace IID.Api.Endpoints.GetAgingStockEndpoint.Validators;

/// <summary>
/// HTTP-shape validation for <see cref="GetAgingStockRequest"/>.
/// Mirrors Sportcast's <c>PriceRequestValidators</c> — query-string shape only.
/// Business rules (which vehicles count as aging stock) live in <c>GetAgingStockQueryHandler</c>.
/// </summary>
public sealed class GetAgingStockRequestValidator : Validator<GetAgingStockRequest>
{
    public GetAgingStockRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.Limit).InclusiveBetween(1, 100);
    }
}
