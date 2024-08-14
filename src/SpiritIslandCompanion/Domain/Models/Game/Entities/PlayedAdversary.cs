using Domain.Models.Static;
using Domain.Primitives;

namespace Domain.Models.Game;

public class PlayedAdversary : Entity<PlayedAdversaryId>
{
    public AdversaryId AdversaryId { get; private set; }
    public AdversaryLevel Level { get; private set; }

    public PlayedAdversary(PlayedAdversaryId id, AdversaryId adversaryId, AdversaryLevel level) : base(id)
    {
        AdversaryId = adversaryId;
        Level = level;
    }
}