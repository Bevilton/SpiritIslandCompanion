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
        return new GamePlayer(id, startingBoard, spiritId, aspectId, userId, null);
    }

    public static GamePlayer CreatePlayer(GamePlayerId id, BoardId startingBoard, SpiritId spiritId, AspectId? aspectId, PlayerId playerId)
    {
        return new GamePlayer(id, startingBoard, spiritId, aspectId, null, playerId);
    }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    /// <summary>
    /// Empty constructor required for EF Core.
    /// </summary>
    [Obsolete("Empty constructor required for EF Core.")]
    private GamePlayer(){}
#pragma warning restore
}