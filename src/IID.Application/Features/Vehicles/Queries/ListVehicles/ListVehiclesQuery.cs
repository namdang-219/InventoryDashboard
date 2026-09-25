namespace IID.Application.Vehicles.Queries.ListVehicles;

public sealed record ListVehiclesQuery(
    string? Make, string? Model,
    int? MinAgeDays, int? MaxAgeDays,
    VehicleStatus? Status,
    int Page = 1,
    int Limit = 20,
    string Sort = "createdAt",
    string Order = "desc",
    Guid? DealershipId = null,
    string? Vin = null,
    string? StockNumber = null)
    : MediatR.IRequest<Domain.Common.Result<Common.Models.PagedResult<Dtos.VehicleResponse>>>;
