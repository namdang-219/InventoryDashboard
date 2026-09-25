using FluentValidation;

namespace IID.Application.Features.Activities.Queries.GetActivities;

public sealed class GetActivitiesQueryValidator : AbstractValidator<GetActivitiesQuery>
{
    public GetActivitiesQueryValidator()
    {
        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 100).WithMessage("Limit must be between 1 and 100.");
    }
}
