using IID.Domain.Common;

namespace IID.Domain.VehicleActions;

/// <summary>
/// A logged proposal or decision on a vehicle, distinct aggregate referencing Vehicle.
/// </summary>
public sealed class VehicleAction : Entity
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

    public static VehicleAction Log(Guid vehicleId, VehicleActionType actionType, string? notes, string loggedByUserId, DateTimeOffset nowUtc)
    {
        if (vehicleId == Guid.Empty) throw new ArgumentException("VehicleId is required.", nameof(vehicleId));
        if (string.IsNullOrWhiteSpace(loggedByUserId)) throw new ArgumentException("LoggedByUserId is required.", nameof(loggedByUserId));
        if (notes is { Length: > 2000 }) throw new ArgumentException("Notes must be ≤ 2000 chars.", nameof(notes));
        return new VehicleAction
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
