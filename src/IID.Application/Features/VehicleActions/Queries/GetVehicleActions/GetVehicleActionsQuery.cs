using IID.Application.Common.Models;
using IID.Application.VehicleActions.Queries.Dtos;
using MediatR;

namespace IID.Application.VehicleActions.Queries.GetVehicleActions;

public sealed record GetVehicleActionsQuery(Guid VehicleId, int Page = 1, int Limit = 50)
    : IRequest<Result<PagedResult<VehicleActionDto>>>;
