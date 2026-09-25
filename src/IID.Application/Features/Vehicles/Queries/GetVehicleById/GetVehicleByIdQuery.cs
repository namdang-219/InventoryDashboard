using IID.Application.Vehicles.Queries.Dtos;
using MediatR;

namespace IID.Application.Vehicles.Queries.GetVehicleById;

public sealed record GetVehicleByIdQuery(Guid Id) : IRequest<Result<VehicleResponse>>;
