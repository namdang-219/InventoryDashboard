using IID.Application.Vehicles.Queries.Dtos;
using MediatR;

namespace IID.Application.Vehicles.Queries.GetAgingStock;

public sealed record GetAgingStockQuery(int Page = 1, int Limit = 20, Guid? DealershipId = null)
    : MediatR.IRequest<Domain.Common.Result<Common.Models.PagedResult<VehicleResponse>>>;
