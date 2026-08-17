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
        var players = Math.Max(1, playerCount);

        int total;
        if (win)
        {
            total = 5 * difficulty
                    + 10
                    + 2 * cards
                    + dahan / players
                    - blight / players
                    + scoreModifier;
        }
        else
        {
            total = 2 * difficulty
                    + cards
                    + dahan / players
                    - blight / players
                    + scoreModifier;
        }

        return Math.Clamp(total, 0, GameRestrictions.MaximumScore);
    }
}
