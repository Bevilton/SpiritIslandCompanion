using Domain.Primitives;
using Domain.Services;

namespace Domain.Models.Game;

/// <summary>
/// How a game ended and what it scored: the outcome the table reported, and the score the game
/// worked out from it. A value rather than an entity: a game has one result, written once (see
/// <see cref="Game.Complete"/>) and never edited, and it is known by what it says rather than
/// by an identity of its own — so two games may hold the very same instance, as a copied game
/// does (see <see cref="Game.CopyForOwner"/>).
/// <para>
/// Only the game makes one. The score is computed from the <see cref="Outcome"/> and the
/// game's own difficulty and table size, so it can never disagree with the game it belongs
/// to — there is no way to hand a game a score.
/// </para>
/// </summary>
public sealed record GameResult : ValueObject
{
    /// <summary>What the table reported when the game ended.</summary>
    public GameOutcome Outcome { get; private init; }

    /// <summary>What that outcome scored, against the game it ended.</summary>
    public Score Score { get; private init; }

    private GameResult(GameOutcome outcome, Score score)
    {
        Outcome = outcome;
        Score = score;
    }

    /// <summary>The outcome scored against the game it ended — see <see cref="ScoreCalculator"/>.</summary>
    internal static GameResult Of(GameOutcome outcome, Difficulty difficulty, int playerCount) =>
        new(outcome, ScoreCalculator.Calculate(outcome, difficulty, playerCount));

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    /// <summary>
    /// Empty constructor required for EF Core.
    /// </summary>
    [Obsolete("Empty constructor required for EF Core.")]
    private GameResult()
    {
    }
#pragma warning restore
}
