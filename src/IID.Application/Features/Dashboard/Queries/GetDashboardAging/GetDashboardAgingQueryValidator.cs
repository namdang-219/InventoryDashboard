using FluentValidation;

namespace IID.Application.Dashboard.Queries.GetDashboardAging;

public sealed class GetDashboardAgingQueryValidator : AbstractValidator<GetDashboardAgingQuery>
{
    public GetDashboardAgingQueryValidator()
    {
        RuleFor(x => x.MinAgeDays)
            .GreaterThanOrEqualTo(0).WithMessage("MinAgeDays cannot be negative.");

        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Page must be at least 1.");

        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 100).WithMessage("Limit must be between 1 and 100.");
    }
}
