using IID.Domain.Common;

namespace IID.Domain.VehicleActions.Events;

public sealed record VehicleActionLogged(
    Guid ActionId,
    Guid VehicleId,
    VehicleActionType ActionType,
    string LoggedByUserId) : IDomainEvent
{
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}
