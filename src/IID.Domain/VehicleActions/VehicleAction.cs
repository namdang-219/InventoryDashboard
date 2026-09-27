using IID.Domain.Common;
using IID.Domain.VehicleActions.Events;

namespace IID.Domain.VehicleActions;

/// <summary>
/// A logged proposal or decision on a vehicle, distinct aggregate root referencing Vehicle.
/// </summary>
public sealed class VehicleAction : AggregateRoot
{
    public Guid VehicleId { get; private set; }
    public VehicleActionType ActionType { get; private set; }
    public string? Notes { get; private set; }
    public string LoggedByUserId { get; private set; } = string.Empty;
    public DateTimeOffset LoggedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public string? CreatedByUserId { get; private set; }
    public string? UpdatedByUserId { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    private VehicleAction() { }

    /// <summary>
    /// Lightweight factory for tests and event-payload assembly. Constructs
    /// an instance whose property shape satisfies the realtime DTO mapper
    /// without needing a full validation pass. Does NOT raise any domain event.
    /// </summary>
    public static VehicleAction ForTesting(
        Guid id,
        Guid vehicleId,
        VehicleActionType actionType = VehicleActionType.Other,
        string? notes = null,
        string loggedByUserId = "tester",
        DateTimeOffset? loggedAt = null)
        => new()
        {
            Id = id,
            VehicleId = vehicleId,
            ActionType = actionType,
            Notes = notes,
            LoggedByUserId = loggedByUserId,
            LoggedAt = loggedAt ?? DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = loggedByUserId,
            UpdatedByUserId = loggedByUserId,
        };

    public static VehicleAction Log(Guid vehicleId, VehicleActionType actionType, string? notes, string loggedByUserId, DateTimeOffset nowUtc)
    {
        if (vehicleId == Guid.Empty) throw new ArgumentException("VehicleId is required.", nameof(vehicleId));
        if (string.IsNullOrWhiteSpace(loggedByUserId)) throw new ArgumentException("LoggedByUserId is required.", nameof(loggedByUserId));
        if (notes is { Length: > 2000 }) throw new ArgumentException("Notes must be ≤ 2000 chars.", nameof(notes));
        var action = new VehicleAction
        {
            Id = Guid.NewGuid(),
            VehicleId = vehicleId,
            ActionType = actionType,
            Notes = notes?.Trim(),
            LoggedByUserId = loggedByUserId,
            LoggedAt = nowUtc,
            CreatedAt = nowUtc,
            UpdatedAt = nowUtc,
            CreatedByUserId = loggedByUserId,
            UpdatedByUserId = loggedByUserId
        };
        // Factory raises the event with the action only — Vehicle is null until
        // a calling command handler enriches it via EnrichLastActionEventWith().
        action.RaiseDomainEvent(new VehicleActionLogged(
            action,
            actionType,
            loggedByUserId));
        return action;
    }

    /// <summary>
    /// Replaces the most-recent <see cref="VehicleActionLogged"/> event on this
    /// aggregate with one that carries the provided <paramref name="vehicle"/>
    /// reference (the originating <see cref="Vehicles.Vehicle"/> the handler
    /// has just loaded). Call this from a command handler after
    /// <see cref="Log"/> and before <c>SaveChangesAsync</c>. Downstream
    /// <c>IDomainEventHandler</c>s serialize the action + vehicle without
    /// re-querying the DB.
    /// </summary>
    public void EnrichLastActionEventWith(Domain.Vehicles.Vehicle vehicle)
    {
        ReplaceLastEvent<VehicleActionLogged>(new VehicleActionLogged(
            this,
            ActionType,
            LoggedByUserId)
        { Vehicle = vehicle });
    }

    public void Update(VehicleActionType actionType, string? notes, DateTimeOffset nowUtc, string updatedByUserId)
    {
        if (notes is { Length: > 2000 }) throw new ArgumentException("Notes must be ≤ 2000 chars.", nameof(notes));
        ActionType = actionType;
        Notes = notes?.Trim();
        UpdatedAt = nowUtc;
        UpdatedByUserId = updatedByUserId;
    }

    public void SoftDelete(DateTimeOffset nowUtc, string deletedByUserId)
    {
        DeletedAt = nowUtc;
        UpdatedAt = nowUtc;
        UpdatedByUserId = deletedByUserId;
    }
}
