using MediatR;

namespace IID.Domain.Common;

/// <summary>
/// Domain event marker that also implements MediatR INotification for in-process publishing.
/// </summary>
public interface IDomainEvent : INotification
{
    DateTime OccurredOnUtc { get; }
}
