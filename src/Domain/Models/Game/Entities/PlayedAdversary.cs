using Domain.Models.Static;
using Domain.Primitives;

namespace Domain.Models.Game;

public class PlayedAdversary : Entity<PlayedAdversaryId>
{
    public AdversaryId AdversaryId { get; private init; }
    public AdversaryLevel Level { get; private init; }

    // Identified by the game alone: nothing outside it refers to a played adversary by id, so
    // the id is minted here rather than asked of the caller.
    private PlayedAdversary(AdversaryId adversaryId, AdversaryLevel level) : base(new PlayedAdversaryId(Guid.NewGuid()))
    {
        AdversaryId = adversaryId;
        Level = level;
    }

    public static PlayedAdversary Create(AdversaryId adversaryId, AdversaryLevel level) => new(adversaryId, level);

    /// <summary>The same adversary at the same level, as an entity of another game.</summary>
    internal PlayedAdversary Copy() => new(AdversaryId, Level);

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    /// <summary>
    /// Empty constructor required for EF Core.
    /// </summary>
    [Obsolete("Empty constructor required for EF Core.")]
    private PlayedAdversary()
    {
    }
#pragma warning restore
}
