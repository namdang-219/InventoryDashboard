using FastEndpoints;
using FluentValidation;
using IID.Domain.Vehicles;

namespace IID.Api.Endpoints.ListVehiclesEndpoint.Validators;

/// <summary>
/// HTTP-shape validation for <see cref="ListVehiclesRequest"/>.
/// Mirrors Sportcast's <c>PriceRequestValidators</c> — query-string shape only.
/// Business rules (sortable columns, status transitions) live in <c>ListVehiclesQueryHandler</c>.
/// </summary>
public sealed class ListVehiclesRequestValidator : Validator<ListVehiclesRequest>
{
    // Whitelist of sortable columns — keep in sync with the LINQ OrderBy in ListVehiclesQueryHandler.
    private static readonly string[] AllowedSortFields =
    {
        "createdAt", "updatedAt", "askingPrice", "purchasePrice", "mileage", "dateAddedToInventory"
    };

    private static readonly string[] AllowedOrderValues = { "asc", "desc" };

    public ListVehiclesRequestValidator()
    {
        RuleFor(x => x.Make).MaximumLength(64);
        RuleFor(x => x.Model).MaximumLength(64);

        RuleFor(x => x.MinAgeDays).GreaterThanOrEqualTo(0).When(x => x.MinAgeDays.HasValue);
        RuleFor(x => x.MaxAgeDays).GreaterThanOrEqualTo(0).When(x => x.MaxAgeDays.HasValue);
        RuleFor(x => x)
            .Must(x => !x.MinAgeDays.HasValue || !x.MaxAgeDays.HasValue || x.MinAgeDays <= x.MaxAgeDays)
            .WithMessage("minAgeDays must be ≤ maxAgeDays.");

        RuleFor(x => x.Status)
            .Must(s => s is null || Enum.TryParse<VehicleStatus>(s, ignoreCase: true, out _))
            .WithMessage($"status must be one of: {string.Join(", ", Enum.GetNames<VehicleStatus>())}.");

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
