using MediatR;

namespace IID.Application.Features.Activities.Queries.GetActivities;

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

public sealed record GetActivitiesResult(
    IReadOnlyList<ActivityItemDto> Items,
    string? NextCursor,
    bool HasMore,
    int Total,
    int UnreadCount);

public sealed record GetActivitiesQuery(int Limit = 10, string? Cursor = null)
    : IRequest<Result<GetActivitiesResult>>;
