using IID.Application.Common.Models;
using IID.Application.Dashboard.Queries.Dtos;
using MediatR;

namespace IID.Application.Dashboard.Queries.GetDashboardBundle;

/// <summary>
/// GET /api/v1/dashboard — returns a full dashboard bundle:
/// summary, quick-stats, chart data, action items, AI insights, and paginated inventory.
/// </summary>
public sealed record GetDashboardBundleQuery(
    int Page = 1,
    int PageSize = 20,
    Guid? DealershipId = null) : IRequest<Result<DashboardBundleDto>>;
