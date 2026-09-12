using Domain.Models.Game;
using Domain.Services;

namespace WebApp.Components.Shared.Games;

/// <summary>
/// What a game's difficulty is made of, itemised — the same terms
/// <see cref="DifficultyCalculator.Compute"/> adds up, in the order it adds them.
/// <para>
/// Two screens show this: the setup form's difficulty dial drops it as a list, and a recorded
/// game's setup card prints it beside the total. Each had its own copy of "which things count",
/// so the calculator's formula was spelled out in three places — and a fourth term added to the
/// domain would have shown up in the number without showing up in either explanation of it.
/// They had already drifted on the wording: the same manual adjustment was "Manual modifier" on
/// one screen and "Modifier" on the other.
/// </para>
/// <para>
/// The lines are data, not markup: the two hosts draw them very differently — a two-column list
/// in a dropdown, a right-aligned run in a hero panel — and only what each line <em>says</em> is
/// shared.
/// </para>
/// </summary>
public static class DifficultyBreakdown
{
    /// <param name="adversaries">The foes at the table, as id and escalation level. Ones the
    /// catalogue no longer lists still count: the game was played against something.</param>
    /// <param name="extraBoardLetter">
    /// Names the extra board where the reader is choosing it — the setup screen, where "which
    /// letter?" is a live question. Null on a recorded game, whose setup card names the board in
    /// a chip of its own.
    /// </param>
    public static IReadOnlyList<BreakdownLine> Of(
        IEnumerable<(string AdversaryId, int Level)> adversaries,
        string? scenarioId,
        bool extraBoard,
        string? extraBoardLetter,
        bool thematicMaps,
        int modifier)
    {
        var lines = new List<BreakdownLine>();

        foreach (var (adversaryId, level) in adversaries)
        {
            var adversary = GameLookups.AdversaryFor(adversaryId);
            var mode = adversary?.Modes.FirstOrDefault(m => m.Level == level);
            lines.Add(new BreakdownLine($"{adversary?.Name ?? "Adversary"} L{level}", mode?.Difficulty ?? 0));
        }

        if (GameLookups.ScenarioFor(scenarioId) is { } scenario)
        {
            lines.Add(new BreakdownLine(scenario.Name, scenario.Difficulty));
        }

        if (extraBoard)
        {
            var label = extraBoardLetter is null ? "Extra board" : $"Extra board ({extraBoardLetter})";
            lines.Add(new BreakdownLine(label, GameRestrictions.ExtraBoardDifficultyBonus));
        }

        if (thematicMaps)
        {
            lines.Add(new BreakdownLine("Thematic maps", GameRestrictions.ThematicMapsDifficultyBonus));
        }

        if (modifier != 0)
        {
            lines.Add(new BreakdownLine("Manual modifier", modifier));
        }

        return lines;
    }
}
