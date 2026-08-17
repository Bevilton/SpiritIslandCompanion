using Domain.Models.User;
using Domain.Primitives;

namespace Domain.Models.Player;

public class Player : AggregateRoot<PlayerId>
{
    public PlayerName Name { get; private set; }
    public UserId CreatedBy { get; private init; }

    private Player(PlayerId id, PlayerName name, UserId createdBy) : base(id)
    {
        Name = name;
        CreatedBy = createdBy;
    }

    public static Player Create(PlayerId id, PlayerName name, UserId createdBy)
    {
        // A private copy: CreatedBy is an owned value, tracked by object identity, and one
        // operation can create several players for the same owner (see SharedGameSplitter).
        // Sharing the instance would read as it moving from one player to the next.
        return new Player(id, name, createdBy with { });
    }

    public void Rename(PlayerName name)
    {
        Name = name;
    }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    /// <summary>
    /// Empty constructor required for EF Core.
    /// </summary>
    [Obsolete("Empty constructor required for EF Core.")]
    private Player(){}
#pragma warning restore
}