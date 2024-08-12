using Domain.Models.Static;
using Domain.Primitives;

namespace Domain.Models.Game;

public class PlayedAdversary : Entity<PlayedAdversaryId>
{
    public AdversaryId AdversaryId { get; private set; }
    public uint AdversaryLevel { get; private set; }

    public PlayedAdversary(PlayedAdversaryId id, AdversaryId adversaryId, uint adversaryLevel) : base(id)
    {
        AdversaryId = adversaryId;
        AdversaryLevel = adversaryLevel;
    }
}