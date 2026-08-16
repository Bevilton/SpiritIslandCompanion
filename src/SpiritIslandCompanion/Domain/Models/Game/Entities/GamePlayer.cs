using Domain.Models.Player;
using Domain.Models.Static;
using Domain.Models.User;
using Domain.Primitives;

namespace Domain.Models.Game;

public class GamePlayer : Entity<GamePlayerId>
{
    public SpiritId SpiritId { get; private set; }
    public AspectId? AspectId { get; private set; }
    public BoardId StartingBoard { get; private set; }
    public UserId? UserId { get; private set; }
    public PlayerId? PlayerId { get; private set; }

    private GamePlayer(GamePlayerId id, BoardId startingBoard, SpiritId spiritId, AspectId? aspectId, UserId? userId, PlayerId? playerId) : base(id)
    {
        SpiritId = spiritId;
        AspectId = aspectId;
        StartingBoard = startingBoard;
        UserId = userId;
        PlayerId = playerId;
    }

    public static GamePlayer CreateUserPlayer(GamePlayerId id, BoardId startingBoard, SpiritId spiritId, AspectId? aspectId, UserId userId)
    {
        return new GamePlayer(id, startingBoard, spiritId, aspectId, Own(userId), null);
    }

    public static GamePlayer CreatePlayer(GamePlayerId id, BoardId startingBoard, SpiritId spiritId, AspectId? aspectId, PlayerId playerId)
    {
        return new GamePlayer(id, startingBoard, spiritId, aspectId, null, Own(playerId));
    }

    /// <summary>
    /// Hands the seat to a registered account — the local player who sat here turned out to
    /// be them (see <see cref="PlayerMerge.PlayerMergeRequest"/>). What was played stays;
    /// only who played it changes.
    /// </summary>
    public void ReassignToUser(UserId userId)
    {
        UserId = Own(userId);
        PlayerId = null;
    }

    /// <summary>
    /// Hands the seat back to a local player — what happens to an account's seat when the
    /// friendship that let it sit there ends. The record of the evening survives; the link
    /// to the other account does not.
    /// </summary>
    public void ReassignToLocalPlayer(PlayerId playerId)
    {
        PlayerId = Own(playerId);
        UserId = null;
    }

    /// <summary>
    /// A private copy of a value the caller may be handing to several seats at once — which
    /// happens whenever a batch of seats is rewritten to the same person.
    /// <para>
    /// Who sits in a seat is stored as an owned type, and the persistence layer tracks owned
    /// values by object identity: giving one instance to two seats reads as that value moving
    /// from the first to the second, leaving the first seat naming nobody. Copying here rather
    /// than at each call site means no caller has to know that.
    /// </para>
    /// </summary>
    private static UserId Own(UserId value) => value with { };

    /// <inheritdoc cref="Own(UserId)"/>
    private static PlayerId Own(PlayerId value) => value with { };

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    /// <summary>
    /// Empty constructor required for EF Core.
    /// </summary>
    [Obsolete("Empty constructor required for EF Core.")]
    private GamePlayer(){}
#pragma warning restore
}