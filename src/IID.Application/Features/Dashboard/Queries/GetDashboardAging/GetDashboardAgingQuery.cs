using IID.Application.Common.Models;
using IID.Application.Dashboard.Queries.Dtos;
using MediatR;

namespace IID.Application.Dashboard.Queries.GetDashboardAging;

/// <summary>
/// GET /api/v1/dashboard/aging — returns vehicles that have been on the lot
/// longer than the specified threshold, sorted by days in inventory descending.
/// </summary>
public sealed record GetDashboardAgingQuery(
    int MinAgeDays = 60,
    int Page = 1,
    int Limit = 20,
    Guid? DealershipId = null) : IRequest<Result<PagedResult<AgingStockItemDto>>>;
