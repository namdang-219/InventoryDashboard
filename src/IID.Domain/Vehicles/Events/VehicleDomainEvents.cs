using IID.Domain.Common;
using IID.Domain.Vehicles;

namespace IID.Domain.Vehicles.Events;

/// <summary>
/// Lifecycle domain events raised by <see cref="Vehicle"/>.
/// Each event carries the full aggregate so downstream event handlers
/// (SignalR broadcasters, etc.) can forward it without an extra DB read.
/// </summary>
public sealed record VehicleAdded(
    Vehicle Vehicle,
    string Make,
    string Model,
    VehicleStatus Status) : IDomainEvent
{
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record VehicleUpdated(
    Vehicle Vehicle,
    string Make) : IDomainEvent
{
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record VehicleTransferred(
    Vehicle Vehicle,
    Guid PreviousDealershipId,
    Guid NewDealershipId,
    string Make) : IDomainEvent
{
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record VehicleStatusChanged(
    Vehicle Vehicle,
    VehicleStatus PreviousStatus,
    VehicleStatus NewStatus,
    string Make) : IDomainEvent
{
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record VehicleSold(
    Vehicle Vehicle,
    string Make,
    DateTimeOffset SoldAtUtc,
    int DaysToSell) : IDomainEvent
{
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record VehicleRemoved(
    Vehicle Vehicle,
    string Make) : IDomainEvent
{
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}
