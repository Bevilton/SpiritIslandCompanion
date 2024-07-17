namespace Domain.Primitives;

public abstract class Entity<TId> where TId : Identifier
{
    protected Entity(TId id)
    {
        Id = id;
    }

    public TId Id { get; private init; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    /// <summary>
    /// Empty constructor required for EF Core.
    /// </summary>
    [Obsolete("Empty constructor required for EF Core.")]
    protected Entity(){}
#pragma warning restore
}