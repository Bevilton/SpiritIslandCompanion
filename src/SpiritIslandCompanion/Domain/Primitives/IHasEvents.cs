namespace Domain.Primitives;

public interface IHasEvents
{
    public IReadOnlyCollection<IDomainEvent> GetDomainEvents();
    void ClearEvents();
}