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
        var total = Compute(scenarioDifficulty, adversaryDifficulties, usesExtraBoard, usesThematicMaps, modifier.Value);
        return Difficulty.Create(total);
    }

    /// <summary>
    /// Raw-int version of the difficulty formula. Use this for UI previews where you don't
    /// want to construct value objects. Always clamped to <c>[0, MaximumDifficulty]</c>.
    /// </summary>
    public static int Compute(
        int scenarioDifficulty,
        IEnumerable<int> adversaryDifficulties,
        bool usesExtraBoard,
        bool usesThematicMaps,
        int modifier)
    {
        var total = scenarioDifficulty
                    + adversaryDifficulties.Sum()
                    + (usesExtraBoard ? GameRestrictions.ExtraBoardDifficultyBonus : 0)
                    + (usesThematicMaps ? GameRestrictions.ThematicMapsDifficultyBonus : 0)
                    + modifier;

        return Math.Clamp(total, 0, GameRestrictions.MaximumDifficulty);
    }
}
