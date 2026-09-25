using IID.Application.Common.Interfaces;
using IID.Application.Common.Models;
using IID.Application.VehicleActions.Queries.Dtos;
using MediatR;

namespace IID.Application.VehicleActions.Queries.GetVehicleActions;

public sealed class GetVehicleActionsHandler(
    IVehicleActionRepository actions,
    IUserDisplayNameProvider userNames) : IRequestHandler<GetVehicleActionsQuery, Result<PagedResult<VehicleActionDto>>>
{
    public async Task<Result<PagedResult<VehicleActionDto>>> Handle(GetVehicleActionsQuery q, CancellationToken ct)
    {
        var page = Math.Max(1, q.Page);
        var limit = Math.Clamp(q.Limit, 1, 100);

        var (items, total) = await actions.ListAsync(q.VehicleId, page, limit, ct);

        var dtos = new List<VehicleActionDto>(items.Count);
        foreach (var a in items)
        {
            var displayName = await userNames.GetDisplayNameAsync(a.LoggedByUserId, ct);
            dtos.Add(new VehicleActionDto(
                a.Id,
                a.VehicleId,
                a.ActionType.ToString(),
                a.Notes,
                a.LoggedByUserId,
                displayName,
                a.LoggedAt,
                a.CreatedAt));
        }

        return Result<PagedResult<VehicleActionDto>>.Success(new PagedResult<VehicleActionDto>(dtos, page, limit, total));
    }
}
