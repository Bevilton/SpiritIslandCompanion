using Domain.Models.Game;
using Domain.Models.Game.Enums;
using Domain.Results;

namespace Domain.Services;

/// <summary>
/// Calculates the game score based on the official Spirit Island scoring rules.
/// Score = Difficulty + Dahan + InvaderCards remaining in deck - Blight + TerrorLevelBonus + ScoreModifier.
/// A loss subtracts a fixed penalty instead of adding the terror bonus.
/// </summary>
public static class ScoreCalculator
{
    public static Result<Score> Calculate(
        bool win,
        Difficulty difficulty,
        DahanCount dahan,
        CardsCount cards,
        BlightCount blight,
        TerrorLevel terrorLevel,
        ScoreModifier scoreModifier)
    {
        var terrorBonus = win ? GetTerrorLevelBonus(terrorLevel) : GetLossPenalty();

        var total = difficulty.Value
                    + dahan.Value
                    + cards.Value
                    - blight.Value
                    + terrorBonus
                    + scoreModifier.Value;

        return Score.Create(Math.Max(0, total));
    }

    private static int GetTerrorLevelBonus(TerrorLevel terrorLevel) => terrorLevel switch
    {
        TerrorLevel.First => 5,
        TerrorLevel.Second => 10,
        TerrorLevel.Third => 15,
        TerrorLevel.Max => 20,
        _ => 0
    };

    private static int GetLossPenalty() => -5;
}
