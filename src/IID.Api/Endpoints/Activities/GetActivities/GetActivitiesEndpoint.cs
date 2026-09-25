using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using IID.Application.Common.Interfaces;

namespace IID.Api.Endpoints.Activities.GetActivities;

public sealed record GetActivitiesRequest
{
    [QueryParam] public int Limit { get; set; } = 10;
    [QueryParam] public string? Cursor { get; set; }
}

public sealed record ActivityItemDto(
    string Id,
    string Type,
    string Title,
    string Detail,
    DateTimeOffset Timestamp,
    string? Severity,
    string? VehicleId,
    string? VehicleName,
    bool IsRead);

public sealed record GetActivitiesResponse(
    List<ActivityItemDto> Items,
    string? NextCursor,
    bool HasMore,
    int Total,
    int UnreadCount);

public sealed class GetActivitiesEndpoint(
    IVehicleActionRepository actions,
    IVehicleRepository vehicles,
    IUserActivityReadRepository readRepo,
    ICurrentUser currentUser) : Endpoint<GetActivitiesRequest, GetActivitiesResponse>
{
    public override void Configure()
    {
        Get("/api/v1/activities");
        Roles("Manager", "Viewer");
        Description(x => x.WithTags("Activities"));
    }

    public override async Task HandleAsync(GetActivitiesRequest req, CancellationToken ct)
    {
        var userId = currentUser.Id ?? string.Empty;
        var readIds = await readRepo.GetReadActivityIdsAsync(userId, ct);

        // Fetch actions using cursor-based pagination
        var limit = req.Limit <= 0 ? 10 : req.Limit;
        var (actionList, nextCursor, hasMore, total) = await actions.ListByCursorAsync(req.Cursor, limit, ct);

        var vehicleIds = actionList.Select(a => a.VehicleId).Distinct().ToList();
        var vehicleMap = new Dictionary<Guid, string>();
        foreach (var vid in vehicleIds)
        {
            var v = await vehicles.GetByIdAsync(vid, ct);
            if (v != null)
            {
                vehicleMap[vid] = $"{v.Year} {v.Make} {v.Model}";
            }
        }

        var result = actionList.Select(a =>
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

        var unreadCount = await readRepo.GetUnreadCountAsync(userId, ct);

        await Send.OkAsync(new GetActivitiesResponse(result, nextCursor, hasMore, total, unreadCount), ct);
    }
}
