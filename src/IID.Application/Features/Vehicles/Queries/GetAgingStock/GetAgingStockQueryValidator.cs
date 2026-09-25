using FluentValidation;

namespace IID.Application.Vehicles.Queries.GetAgingStock;

public sealed class GetAgingStockQueryValidator : AbstractValidator<GetAgingStockQuery>
{
    public GetAgingStockQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.Limit).InclusiveBetween(1, 100);
    }
}
