using IID.Domain.Vehicles;

namespace IID.Application.Common.Models;

/// <summary>
/// Parameters for querying and filtering vehicles with pagination and sorting.
/// </summary>
public sealed record VehicleListFilter(
    string? Make = null,
    string? Model = null,
    int? MinAgeDays = null,
    int? MaxAgeDays = null,
    VehicleStatus? Status = null,
    int Page = 1,
    int Limit = 20,
    string Sort = "createdAt",
    string Order = "desc",
    Guid? DealershipId = null,
    string? Vin = null,
    string? StockNumber = null);
