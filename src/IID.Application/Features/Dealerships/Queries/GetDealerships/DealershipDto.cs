using IID.Domain.Dealerships;

namespace IID.Application.Features.Dealerships.Queries.GetDealerships;

public sealed record DealershipDto(
    Guid Id,
    string Name,
    string Code,
    string City,
    string State,
    string Phone,
    int VehicleCount)
{
    public static DealershipDto From(Dealership d)
        => new(d.Id, d.Name, d.Code, d.City, d.State, d.Phone, d.Vehicles.Count);
}
