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

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    /// <summary>
    /// Empty constructor required for EF Core.
    /// </summary>
    [Obsolete("Empty constructor required for EF Core.")]
    private PlayedAdversary(){}
#pragma warning restore
}