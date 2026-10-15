using Domain.Errors;
using Domain.Primitives;
using Domain.Results;

namespace Domain.Models.Game;

/// <summary>
/// What the table reports when a game ends: won or lost, how long it took, and the counts the
/// score is made from. The score itself is not here — it depends on the game's difficulty and
/// table size, so the game computes it when it takes the outcome (see <see cref="Game.Complete"/>),
/// and what it keeps is a <see cref="GameResult"/>.
/// </summary>
public sealed record GameOutcome : ValueObject
{
    public bool Win { get; private init; }
    public TimeSpan Duration { get; private init; }
    public CardsCount Cards { get; private init; }
    public TerrorLevel TerrorLevel { get; private init; }
    public BlightCount Blight { get; private init; }
    public DahanCount Dahan { get; private init; }
    public ScoreModifier ScoreModifier { get; private init; }

    private GameOutcome(
        bool win,
        TimeSpan duration,
        CardsCount cards,
        TerrorLevel terrorLevel,
        BlightCount blight,
        DahanCount dahan,
        ScoreModifier scoreModifier)
    {
        Win = win;
        Duration = duration;
        Cards = cards;
        TerrorLevel = terrorLevel;
        Blight = blight;
        Dahan = dahan;
        ScoreModifier = scoreModifier;
    }

    public static Result<GameOutcome> Create(
        bool win,
        TimeSpan duration,
        CardsCount cards,
        TerrorLevel terrorLevel,
        BlightCount blight,
        DahanCount dahan,
        ScoreModifier scoreModifier)
    {
        if (duration < TimeSpan.Zero)
            return Result.Failure<GameOutcome>(DomainErrors.Game.DurationNegative);
        if (duration >= GameRestrictions.MaximumDuration)
            return Result.Failure<GameOutcome>(DomainErrors.Game.DurationTooLong);
        if (!Enum.IsDefined(terrorLevel))
            return Result.Failure<GameOutcome>(DomainErrors.Game.InvalidTerrorLevel);

        return new GameOutcome(win, duration, cards, terrorLevel, blight, dahan, scoreModifier);
    }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    /// <summary>
    /// Empty constructor required for EF Core.
    /// </summary>
    [Obsolete("Empty constructor required for EF Core.")]
    private GameOutcome()
    {
    }
#pragma warning restore
}
