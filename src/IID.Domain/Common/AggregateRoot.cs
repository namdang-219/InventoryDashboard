namespace IID.Domain.Common;

public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>
    /// Replaces the most-recently-raised event of type <typeparamref name="TEvent"/>
    /// on this aggregate. Used by aggregates that build events from a factory with
    /// limited context (only ids) and need to enrich the event with the full
    /// aggregate reference once more is known.
    /// </summary>
    protected void ReplaceLastEvent<TEvent>(TEvent replacement) where TEvent : IDomainEvent
    {
        for (var i = _domainEvents.Count - 1; i >= 0; i--)
        {
            if (_domainEvents[i] is TEvent)
            {
                _domainEvents[i] = replacement;
                return;
            }
        }
    }
}
