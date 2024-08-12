using Domain.Models.Player;
using Domain.Models.Static;

namespace Domain.Models.Game;

public class UnregisteredPlayer : GamePlayer
{
    public PlayerId PlayerId { get; private init; }

    public UnregisteredPlayer(GamePlayerId id, PlayerId playerId, BoardId startingBoard, SpiritId spiritId, AspectId? aspectId) : base(id, startingBoard, spiritId, aspectId)
    {
        PlayerId = playerId;
    }
}