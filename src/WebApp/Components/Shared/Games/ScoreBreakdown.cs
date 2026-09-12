using Domain.Services;

namespace WebApp.Components.Shared.Games;

/// <summary>
/// What a score is made of, itemised — the terms <see cref="ScoreCalculator.Compute"/> adds up,
/// in the order it adds them, for the live preview under the result editor. The twin of
/// <see cref="DifficultyBreakdown"/>, and for the same reason: the formula lived in the editor's
/// markup as arithmetic beside the calculator's own, so a change to the scoring rule would have
/// moved the number and left the explanation of it behind. Every weight and every per-player
/// share is the calculator's own member, so the two cannot disagree.
/// <para>
/// The lines are not clamped the way the total is — a defeat under heavy blight can add up to
/// less than nothing, and the calculator floors it at zero. The lines say what happened; the
/// figure beside them says what it counts for.
/// </para>
/// </summary>
public static class ScoreBreakdown
{
    public static IReadOnlyList<BreakdownLine> Of(GameResultDraft result, int difficulty, int playerCount)
    {
        var players = Math.Max(1, playerCount);
        var lines = new List<BreakdownLine>();

        if (result.Win)
        {
            lines.Add(new($"{ScoreCalculator.WinDifficultyWeight} × difficulty ({difficulty})",
                ScoreCalculator.WinDifficultyWeight * difficulty));
            lines.Add(new("Victory bonus", ScoreCalculator.VictoryBonus));
            lines.Add(new($"{ScoreCalculator.RemainingCardWeight} × cards remaining ({result.Cards})",
                ScoreCalculator.RemainingCardWeight * result.Cards));
        }
        else
        {
            lines.Add(new($"{ScoreCalculator.LossDifficultyWeight} × difficulty ({difficulty})",
                ScoreCalculator.LossDifficultyWeight * difficulty));
            lines.Add(new($"Cards used ({result.Cards})", ScoreCalculator.UsedCardWeight * result.Cards));
        }

        lines.Add(new($"Dahan {result.Dahan} / {players}", ScoreCalculator.PerPlayer(result.Dahan, playerCount)));
        lines.Add(new($"Blight {result.Blight} / {players}", -ScoreCalculator.PerPlayer(result.Blight, playerCount)));

        if (result.ScoreModifier != 0)
        {
            lines.Add(new("Modifier", result.ScoreModifier));
        }

        return lines;
    }
}
