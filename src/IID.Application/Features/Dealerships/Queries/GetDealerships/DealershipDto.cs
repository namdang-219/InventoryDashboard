namespace IID.Application.Features.Dealerships.Queries.GetDealerships;

public sealed record DealershipDto(
    Guid Id,
    string Name,
    string Code,
    string City,
    string State,
    string Phone,
    int VehicleCount);
