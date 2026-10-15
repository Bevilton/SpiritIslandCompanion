using Domain.Models.Player;
using Domain.Models.Static;
using Domain.Models.User;
using Domain.Primitives;

namespace Domain.Models.Game;

public class GamePlayer : Entity<GamePlayerId>
{
    public SpiritId SpiritId { get; private init; }
    public AspectId? AspectId { get; private init; }
    public BoardId StartingBoard { get; private init; }
    public UserId? UserId { get; private set; }
    public PlayerId? PlayerId { get; private set; }

    // A seat's identity is its own to mint: nothing outside the game ever refers to a seat by
    // id, so there is no reason for a caller to supply one.
    private GamePlayer(BoardId startingBoard, SpiritId spiritId, AspectId? aspectId, UserId? userId, PlayerId? playerId)
        : base(new GamePlayerId(Guid.NewGuid()))
    {
        SpiritId = spiritId;
        AspectId = aspectId;
        StartingBoard = startingBoard;
        UserId = userId;
        PlayerId = playerId;
    }

    /// <summary>A seat taken by a registered account — the owner's own, or a friend's.</summary>
    public static GamePlayer ForUser(BoardId startingBoard, SpiritId spiritId, AspectId? aspectId, UserId userId) =>
        new(startingBoard, spiritId, aspectId, userId, null);

    /// <summary>A seat taken by one of the owner's local players.</summary>
    public static GamePlayer ForLocalPlayer(BoardId startingBoard, SpiritId spiritId, AspectId? aspectId, PlayerId playerId) =>
        new(startingBoard, spiritId, aspectId, null, playerId);

    /// <summary>
    /// The seat as it reads from <paramref name="newOwner"/>'s side of the table — what a copy
    /// of the game made in their name seats here (see <see cref="Game.CopyForOwner"/>). What was
    /// played is copied as it is; only who is written down changes: their own account stays
    /// theirs, an empty seat stays empty (there is nobody in it to write down), and everyone
    /// else becomes one of their local players — which one, <paramref name="localPlayerFor"/>
    /// decides, since only the caller knows the new owner's address book.
    /// </summary>
    internal GamePlayer CopyFor(UserId newOwner, Func<GamePlayer, PlayerId> localPlayerFor)
    {
        if (UserId == newOwner)
            return ForUser(StartingBoard, SpiritId, AspectId, newOwner);
        if (UserId is null && PlayerId is null)
            return new GamePlayer(StartingBoard, SpiritId, AspectId, null, null);

        return ForLocalPlayer(StartingBoard, SpiritId, AspectId, localPlayerFor(this));
    }

    // Who sits in a seat is the game's to change, never a caller's: the three below are reached
    // only through Game.HandSeatsToUser, HandSeatsToLocalPlayer and UnseatLocalPlayer, which is
    // where the rule about seating an account twice lives.

    /// <summary>Hands the seat to a registered account. What was played stays; only who played it changes.</summary>
    internal void ReassignToUser(UserId userId)
    {
        UserId = userId;
        PlayerId = null;
    }

    /// <summary>Hands the seat to a local player. What was played stays; only who played it changes.</summary>
    internal void ReassignToLocalPlayer(PlayerId playerId)
    {
        PlayerId = playerId;
        UserId = null;
    }

    /// <summary>Empties the seat of whoever sat in it, keeping what they played.</summary>
    internal void Unassign()
    {
        UserId = null;
        PlayerId = null;
    }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    /// <summary>
    /// Empty constructor required for EF Core.
    /// </summary>
    [Obsolete("Empty constructor required for EF Core.")]
    private GamePlayer()
    {
    }
#pragma warning restore
}
