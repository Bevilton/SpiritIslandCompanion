namespace Domain.Primitives;

public abstract class AggregateRoot<TId> : Entity<TId>, IAggregateRoot, IHasEvents where TId : Identifier
{
    private readonly List<IDomainEvent> _domainEvents = new();

    protected AggregateRoot(TId id) : base(id)
    {

    }

    public IReadOnlyCollection<IDomainEvent> GetDomainEvents() => _domainEvents.AsReadOnly();


    public void ClearEvents() => _domainEvents.Clear();

    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    /// <summary>
    /// Empty constructor required for EF Core.
    /// </summary>
    [Obsolete("Empty constructor required for EF Core.")]
    protected AggregateRoot(){}
#pragma warning restore
}