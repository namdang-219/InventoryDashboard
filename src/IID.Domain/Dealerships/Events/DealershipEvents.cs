using IID.Domain.Common;

namespace IID.Domain.Dealerships.Events;

/// <summary>
/// Raised by <see cref="Dealership"/> when a new dealership is created.
/// Carries the full aggregate so downstream event handlers (SignalR
/// broadcaster, etc.) can serialize without an extra DB read.
/// </summary>
public sealed record DealershipAdded(Dealership Dealership) : IDomainEvent
{
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Raised by <see cref="Dealership.Update"/> when an existing dealership
/// is mutated. Carries the full aggregate.
/// </summary>
public sealed record DealershipUpdated(Dealership Dealership) : IDomainEvent
{
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}
