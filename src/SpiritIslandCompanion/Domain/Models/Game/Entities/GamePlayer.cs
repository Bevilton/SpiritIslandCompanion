using Domain.Models.Static;
using Domain.Primitives;

namespace Domain.Models.Game;

public abstract class GamePlayer : Entity<GamePlayerId>
{
    public SpiritId SpiritId { get; protected set; }
    public AspectId? AspectId { get; protected set; }
    public BoardId StartingBoard { get; protected set; }

    public GamePlayer(GamePlayerId id, BoardId startingBoard, SpiritId spiritId, AspectId? aspectId) : base(id)
    {
        SpiritId = spiritId;
        AspectId = aspectId;
        StartingBoard = startingBoard;
    }
}