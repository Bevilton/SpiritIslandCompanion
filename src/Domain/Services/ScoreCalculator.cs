using Domain.Models.Game;
using Domain.Results;

namespace Domain.Services;

/// <summary>
/// Win:  5×difficulty + 10 + 2×cardsRemaining + dahan/players − blight/players + modifier
/// Loss: 2×difficulty + cardsUsed             + dahan/players − blight/players + modifier
/// The cards field stores "remaining" on win and "used" on loss.
/// </summary>
public static class ScoreCalculator
{
    /// <summary>
    /// The formula's weights, named so a preview that explains the score line by line can
    /// quote them rather than repeat them — see the web app's <c>ScoreBreakdown</c>.
    /// </summary>
    public const int WinDifficultyWeight = 5;
    public const int LossDifficultyWeight = 2;
    public const int VictoryBonus = 10;
    public const int RemainingCardWeight = 2;
    public const int UsedCardWeight = 1;

    /// <summary>
    /// A token count shared out across the table — dahan and blight both score per player, in
    /// whole players, and a table of nobody scores as a table of one. Public for the same reason
    /// the weights are: the preview that itemises the score quotes this term rather than
    /// re-deriving it.
    /// </summary>
    public static int PerPlayer(int tokens, int playerCount) => tokens / Math.Max(1, playerCount);

    public static Result<Score> Calculate(
        bool win,
        Difficulty difficulty,
        DahanCount dahan,
        CardsCount cards,
        BlightCount blight,
        int playerCount,
        ScoreModifier scoreModifier)
    {
        var total = Compute(win, difficulty.Value, dahan.Value, cards.Value, blight.Value, playerCount, scoreModifier.Value);
        return Score.Create(total);
    }

    /// <summary>
    /// Raw-int version of the scoring formula. Use this for UI previews where you don't
    /// want to construct value objects (which would fail validation for transient form state).
    /// Always clamped to <c>[0, MaximumScore]</c>, so the result is safe to display directly.
    /// </summary>
    public static int Compute(
        bool win,
        int difficulty,
        int dahan,
        int cards,
        int blight,
        int playerCount,
        int scoreModifier)
    {
        int total;
        if (win)
        {
            total = WinDifficultyWeight * difficulty
                    + VictoryBonus
                    + RemainingCardWeight * cards
                    + PerPlayer(dahan, playerCount)
                    - PerPlayer(blight, playerCount)
                    + scoreModifier;
        }
        else
        {
            total = LossDifficultyWeight * difficulty
                    + UsedCardWeight * cards
                    + PerPlayer(dahan, playerCount)
                    - PerPlayer(blight, playerCount)
                    + scoreModifier;
        }

        return Math.Clamp(total, 0, GameRestrictions.MaximumScore);
    }
}
