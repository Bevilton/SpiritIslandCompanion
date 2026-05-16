using Domain.Models.Game;
using Domain.Results;

namespace Domain.Services;

/// <summary>
/// Total difficulty = scenario + Σ adversary levels + (extra board ? +2) + (thematic ? +3) + modifier.
/// </summary>
public static class DifficultyCalculator
{
    public static Result<Difficulty> Calculate(
        int scenarioDifficulty,
        IEnumerable<int> adversaryDifficulties,
        bool usesExtraBoard,
        bool usesThematicMaps,
        DifficultyModifier modifier)
    {
        var total = scenarioDifficulty
                    + adversaryDifficulties.Sum()
                    + (usesExtraBoard ? GameRestrictions.ExtraBoardDifficultyBonus : 0)
                    + (usesThematicMaps ? GameRestrictions.ThematicMapsDifficultyBonus : 0)
                    + modifier.Value;

        var clamped = Math.Clamp(total, 0, GameRestrictions.MaximumDifficulty);
        return Difficulty.Create(clamped);
    }
}
