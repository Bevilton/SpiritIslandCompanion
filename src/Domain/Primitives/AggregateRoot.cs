namespace Domain.Primitives;

/// <summary>
/// The root of an aggregate: the one entity of a consistency boundary that the application
/// layer loads, changes and saves. Everything inside the boundary — a game's seats and
/// adversaries, a user's expansions — is reached through it and never persisted on its own.
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId> where TId : Identifier
{
    protected AggregateRoot(TId id) : base(id)
    {
    }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    /// <summary>
    /// Empty constructor required for EF Core.
    /// </summary>
    [Obsolete("Empty constructor required for EF Core.")]
    protected AggregateRoot()
    {
    }
#pragma warning restore
}
