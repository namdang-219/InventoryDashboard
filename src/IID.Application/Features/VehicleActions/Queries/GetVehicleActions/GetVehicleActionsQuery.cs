using IID.Application.Common.Models;
using IID.Application.VehicleActions.Queries.Dtos;
using MediatR;

namespace IID.Application.VehicleActions.Queries.GetVehicleActions;

public sealed record GetVehicleActionsQuery(
    Guid? VehicleId = null,
    int Page = 1,
    int Limit = 50,
    string? ActionType = null,
    string? Search = null)
    : IRequest<Result<PagedResult<VehicleActionDto>>>;
