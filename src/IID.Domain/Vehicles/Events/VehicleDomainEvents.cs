using IID.Domain.Common;

namespace IID.Domain.Vehicles.Events;

public sealed record VehicleAdded(
    Guid VehicleId,
    string Make,
    string Model,
    VehicleStatus Status) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}

public sealed record VehicleUpdated(
    Guid VehicleId,
    string Make) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}

public sealed record VehicleStatusChanged(
    Guid VehicleId,
    VehicleStatus PreviousStatus,
    VehicleStatus NewStatus,
    string Make) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}

public sealed record VehicleSold(
    Guid VehicleId,
    string Make,
    DateTimeOffset SoldAtUtc,
    int DaysToSell) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}

public sealed record VehicleRemoved(
    Guid VehicleId,
    string Make) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
