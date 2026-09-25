using IID.Application.Dashboard.Queries.Dtos;
using MediatR;

namespace IID.Application.Dashboard.Queries.GetLowInventoryAlerts;

/// <summary>
/// GET /api/v1/dashboard/alerts/low-inventory — returns alerts for vehicle
/// makes/models that have critically low available inventory.
/// </summary>
public sealed record GetLowInventoryAlertsQuery(Guid? DealershipId = null) : IRequest<Result<IReadOnlyList<LowInventoryAlertDto>>>;
