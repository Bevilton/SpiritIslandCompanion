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
        var players = Math.Max(1, playerCount);

        int total;
        if (win)
        {
            total = 5 * difficulty.Value
                    + 10
                    + 2 * cards.Value
                    + dahan.Value / players
                    - blight.Value / players
                    + scoreModifier.Value;
        }
        else
        {
            total = 2 * difficulty.Value
                    + cards.Value
                    + dahan.Value / players
                    - blight.Value / players
                    + scoreModifier.Value;
        }

        return Score.Create(Math.Max(0, total));
    }
}
