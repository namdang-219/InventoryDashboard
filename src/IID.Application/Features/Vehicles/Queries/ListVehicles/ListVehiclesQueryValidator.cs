using FluentValidation;

namespace IID.Application.Vehicles.Queries.ListVehicles;

public sealed class ListVehiclesQueryValidator : AbstractValidator<ListVehiclesQuery>
{
    private static readonly string[] AllowedSortFields =
    {
        "createdAt", "updatedAt", "askingPrice", "purchasePrice", "mileage", "dateAddedToInventory"
    };

    private static readonly string[] AllowedOrderValues = { "asc", "desc" };

    public ListVehiclesQueryValidator()
    {
        RuleFor(x => x.Make).MaximumLength(64);
        RuleFor(x => x.Model).MaximumLength(64);
        RuleFor(x => x.Vin).MaximumLength(32);
        RuleFor(x => x.StockNumber).MaximumLength(64);

        RuleFor(x => x.MinAgeDays).GreaterThanOrEqualTo(0).When(x => x.MinAgeDays.HasValue);
        RuleFor(x => x.MaxAgeDays).GreaterThanOrEqualTo(0).When(x => x.MaxAgeDays.HasValue);
        RuleFor(x => x)
            .Must(x => !x.MinAgeDays.HasValue || !x.MaxAgeDays.HasValue || x.MinAgeDays <= x.MaxAgeDays)
            .WithMessage("minAgeDays must be <= maxAgeDays.");

        RuleFor(x => x.Status).IsInEnum().When(x => x.Status.HasValue);

        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.Limit).InclusiveBetween(1, 100);

        RuleFor(x => x.Sort)
            .Must(s => AllowedSortFields.Contains(s, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"sort must be one of: {string.Join(", ", AllowedSortFields)}.");

        RuleFor(x => x.Order)
            .Must(o => AllowedOrderValues.Contains(o, StringComparer.OrdinalIgnoreCase))
            .WithMessage("order must be 'asc' or 'desc'.");
    }
}
