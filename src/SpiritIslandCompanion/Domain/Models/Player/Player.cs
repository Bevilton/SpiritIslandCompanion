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
        return new Player(id, name, createdBy);
    }
}