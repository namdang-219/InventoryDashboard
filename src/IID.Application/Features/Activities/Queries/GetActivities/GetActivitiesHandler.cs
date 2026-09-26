using IID.Application.Common.Interfaces;
using MediatR;

namespace IID.Application.Features.Activities.Queries.GetActivities;

public sealed class GetActivitiesHandler(
    IVehicleActionRepository vehicleActionRepository,
    IVehicleRepository vehicleRepository,
    IUserActivityReadRepository userActivityReadRepository,
    ICurrentUser currentUser) : IRequestHandler<GetActivitiesQuery, Result<GetActivitiesResult>>
{
    public async Task<Result<GetActivitiesResult>> Handle(GetActivitiesQuery q, CancellationToken ct)
    {
        var userId = currentUser.Id ?? string.Empty;
        var readIds = await userActivityReadRepository.GetReadActivityIdsAsync(userId, ct);

        // Fetch actions using cursor-based pagination
        var limit = q.Limit <= 0 ? 10 : q.Limit;
        var (actionList, nextCursor, hasMore, total) = await vehicleActionRepository.ListByCursorAsync(q.Cursor, limit, ct);

        var vehicleIds = actionList.Select(a => a.VehicleId).Distinct().ToList();
        var vehicleMap = new Dictionary<Guid, string>();
        if (vehicleIds.Count > 0)
        {
            var batchVehicles = await vehicleRepository.GetByIdsAsync(vehicleIds, ct);
            if (batchVehicles != null)
            {
                foreach (var v in batchVehicles)
                {
                    vehicleMap[v.Id] = $"{v.Year} {v.Make} {v.Model}";
                }
            }

            // Fallback for mocks that only configured GetByIdAsync
            foreach (var vid in vehicleIds.Where(id => !vehicleMap.ContainsKey(id)))
            {
                var v = await vehicleRepository.GetByIdAsync(vid, ct);
                if (v != null)
                {
                    vehicleMap[vid] = $"{v.Year} {v.Make} {v.Model}";
                }
            }
        }

        var items = actionList.Select(a =>
        {
            var id = a.Id.ToString();
            vehicleMap.TryGetValue(a.VehicleId, out var vName);
            return new ActivityItemDto(
                Id: id,
                Type: "action",
                Title: $"Action: {a.ActionType}",
                Detail: a.Notes ?? "No notes provided",
                Timestamp: a.LoggedAt,
                Severity: "info",
                VehicleId: a.VehicleId.ToString(),
                VehicleName: vName,
                IsRead: readIds.Contains(id)
            );
        }).ToList();

        var unreadCount = await userActivityReadRepository.GetUnreadCountAsync(userId, ct);

        return Result<GetActivitiesResult>.Success(
            new GetActivitiesResult(items, nextCursor, hasMore, total, unreadCount));
    }
}
