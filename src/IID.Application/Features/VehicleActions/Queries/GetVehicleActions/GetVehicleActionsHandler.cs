using IID.Application.Common.Interfaces;
using IID.Application.Common.Models;
using IID.Application.VehicleActions.Queries.Dtos;
using MediatR;

namespace IID.Application.VehicleActions.Queries.GetVehicleActions;

public sealed class GetVehicleActionsHandler(
    IVehicleActionRepository actions,
    IVehicleRepository vehicles,
    IUserDisplayNameProvider userNames) : IRequestHandler<GetVehicleActionsQuery, Result<PagedResult<VehicleActionDto>>>
{
    public async Task<Result<PagedResult<VehicleActionDto>>> Handle(GetVehicleActionsQuery q, CancellationToken ct)
    {
        var page = Math.Max(1, q.Page);
        var limit = Math.Clamp(q.Limit, 1, 100);

        var (items, total) = await actions.ListAsync(new VehicleActionListFilter(q.VehicleId, page, limit, q.ActionType, q.Search), ct);

        var vehicleIds = items.Select(a => a.VehicleId).Distinct().ToList();
        var vehicleList = await vehicles.GetByIdsAsync(vehicleIds, ct);
        var vehicleMap = vehicleList.ToDictionary(v => v.Id);

        var dtos = new List<VehicleActionDto>(items.Count);
        foreach (var a in items)
        {
            var displayName = await userNames.GetDisplayNameAsync(a.LoggedByUserId, ct);
            vehicleMap.TryGetValue(a.VehicleId, out var v);

            dtos.Add(new VehicleActionDto(
                a.Id,
                a.VehicleId,
                a.ActionType.ToString(),
                a.Notes,
                a.LoggedByUserId,
                displayName,
                a.LoggedAt,
                a.CreatedAt,
                v?.Vin.Value,
                v?.Make,
                v?.Model,
                v?.Year,
                v?.StockNumber));
        }

        return Result<PagedResult<VehicleActionDto>>.Success(new PagedResult<VehicleActionDto>(dtos, page, limit, total));
    }
}
