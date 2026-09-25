using IID.Application.Dashboard.Dtos;

namespace IID.Application.Dashboard.Queries.GetDashboardSummary;

public sealed record GetDashboardSummaryQuery(DateTimeOffset? AsOfUtc = null)
    : MediatR.IRequest<IID.Domain.Common.Result<DashboardSummaryDto>>;
