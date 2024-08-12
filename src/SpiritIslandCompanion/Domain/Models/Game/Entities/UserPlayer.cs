using Domain.Models.Static;
using Domain.Models.User;

namespace Domain.Models.Game;

public class UserPlayer : GamePlayer
{
    public UserId UserId { get; private init; }

    public UserPlayer(GamePlayerId id, UserId userId, BoardId startingBoard, SpiritId spiritId, AspectId aspectId) : base(id, startingBoard, spiritId, aspectId)
    {
        UserId = userId;
    }
}