using IID.Domain.Common;
using IID.Domain.Vehicles;

namespace IID.Domain.VehicleActions.Events;

/// <summary>
/// Raised by <see cref="VehicleAction"/>. Carries the action aggregate and an
/// optional <see cref="Vehicle"/> reference so downstream <c>IDomainEventHandler</c>
/// implementations can serialize without re-querying the DB.
/// </summary>
public sealed record VehicleActionLogged(
    VehicleAction Action,
    VehicleActionType ActionType,
    string LoggedByUserId) : IDomainEvent
{
    /// <summary>Shortcut for the action's vehicle id.</summary>
    public Guid VehicleId => Action.VehicleId;

    /// <summary>
    /// Originating vehicle. Null when the event was raised from the static
    /// factory and no handler has yet attached the in-scope reference.
    /// </summary>
    public Vehicle? Vehicle { get; init; }

    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}
