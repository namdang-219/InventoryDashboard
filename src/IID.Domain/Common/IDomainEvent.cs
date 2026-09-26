namespace IID.Domain.Common;

/// <summary>
/// Domain event marker for pure domain concepts (zero external framework dependency).
/// </summary>
public interface IDomainEvent
{
    DateTimeOffset OccurredOnUtc { get; }
}
