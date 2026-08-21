using Application.Features.Games.Dtos;
using Domain.Models.Static;
using Domain.Models.Static.Data;
using WebApp.Components.Shared.Stats;

namespace WebApp.Components.Shared.Games;

/// <summary>
/// Lookups from the static <see cref="GameData"/> tables by their string id.
/// Shared by every page/component that renders persisted game data — they all
/// store the entity id and need the name, color, image, etc. for display.
/// </summary>
public static class GameLookups
{
    /// <summary>Fallback swatch for an entity without a catalogue colour of its own.
    /// Kept in step with ink-500 in Styles/site.css, which is the warm parchment neutral.</summary>
    public const string NeutralColor = "#7a6c4e";

    public static Spirit? SpiritFor(string? id) =>
        string.IsNullOrEmpty(id) ? null : GameData.Spirits.FirstOrDefault(x => x.Id.Value == id);

    public static Board? BoardFor(string? id) =>
        string.IsNullOrEmpty(id) ? null : GameData.Boards.FirstOrDefault(x => x.Id.Value == id);

    public static Adversary? AdversaryFor(string? id) =>
        string.IsNullOrEmpty(id) ? null : GameData.Adversaries.FirstOrDefault(x => x.Id.Value == id);

    public static Scenario? ScenarioFor(string? id) =>
        string.IsNullOrEmpty(id) ? null : GameData.Scenarios.FirstOrDefault(x => x.Id.Value == id);

    public static IslandSetup? IslandSetupFor(string? id) =>
        string.IsNullOrEmpty(id) ? null : GameData.IslandSetups.FirstOrDefault(x => x.Id.Value == id);

    public static Aspect? AspectFor(string? id) =>
        string.IsNullOrEmpty(id) ? null : GameData.Aspects.FirstOrDefault(x => x.Id.Value == id);

    public static string ExpansionName(ExpansionId id) =>
        GameData.Expansions.FirstOrDefault(e => e.Id == id)?.Name ?? id.Value;

    /// <summary>A board's letter — from the catalogue detail, or the name's last character
    /// for a board that has no detail entry.</summary>
    public static string BoardLetter(Board board) =>
        BoardDetails.For(board.Id)?.Letter ?? board.Name[^1..];

    /// <summary>
    /// The two letters a spirit wears wherever it shrinks to a badge — the catalogue entry's
    /// hand-picked code, unique across all spirits. Only a spirit without a detail entry (played
    /// from outside the catalogue) falls back to deriving one from its name: first letters of
    /// the first two significant words, or the first two letters of a one-word name.
    /// </summary>
    public static string SpiritMonogram(Spirit spirit) =>
        SpiritDetails.For(spirit.Id)?.Monogram ?? DeriveMonogram(spirit.Name);

    private static string DeriveMonogram(string name)
    {
        var words = name.Split([' ', '-'], StringSplitOptions.RemoveEmptyEntries)
            .Where(w => !MonogramSkipWords.Contains(w))
            .ToList();
        return words.Count switch
        {
            0 => "?",
            1 => words[0][..Math.Min(2, words[0].Length)],
            _ => $"{char.ToUpperInvariant(words[0][0])}{char.ToUpperInvariant(words[1][0])}",
        };
    }

    private static readonly HashSet<string> MonogramSkipWords =
        new(StringComparer.OrdinalIgnoreCase) { "a", "an", "as", "and", "in", "its", "of", "the", "up", "your" };

    /// <summary>
    /// The text colour a label needs on a coloured fill: dark ink on a light colour
    /// (Lightning's yellow drowns white text), paper on a dark one.
    /// </summary>
    public static string InkOn(string hexColor)
    {
        var r = Convert.ToInt32(hexColor.Substring(1, 2), 16);
        var g = Convert.ToInt32(hexColor.Substring(3, 2), 16);
        var b = Convert.ToInt32(hexColor.Substring(5, 2), 16);
        var brightness = (0.299 * r + 0.587 * g + 0.114 * b) / 255;
        return brightness > 0.62 ? StatsTheme.Ink900 : StatsTheme.Paper;
    }

    /// <summary>The fallback <see cref="MatchTitle"/>, for callers that style it differently.</summary>
    public const string StandardGameTitle = "Standard game";

    /// <summary>
    /// What a recorded game is called wherever one is listed: the adversaries fought, or the
    /// scenario when there were none, or "Standard game" when there was neither. One
    /// helper so the games list, the game detail and anything else that names a match agree.
    /// </summary>
    public static string MatchTitle(IReadOnlyList<string> adversaryIds, string? scenarioId)
    {
        var adversaries = adversaryIds
            .Select(id => AdversaryFor(id)?.Name)
            .Where(n => n is not null)
            .ToList();
        if (adversaries.Count > 0) return string.Join(" + ", adversaries);
        return ScenarioFor(scenarioId)?.Name ?? StandardGameTitle;
    }

    /// <summary>
    /// <see cref="MatchTitle(IReadOnlyList{string}, string?)"/> with the levels fought, for
    /// callers whose data carries them: "England L3 + Russia L2".
    /// </summary>
    public static string MatchTitle(IReadOnlyList<GameAdversaryResponse> adversaries, string? scenarioId) =>
        AdversaryLabel(adversaries) ?? ScenarioFor(scenarioId)?.Name ?? StandardGameTitle;

    /// <summary>The adversaries with their levels, "England L3 + Russia L2" — null when none.</summary>
    public static string? AdversaryLabel(IReadOnlyList<GameAdversaryResponse> adversaries)
    {
        var named = adversaries
            .Select(a => AdversaryFor(a.AdversaryId) is { } adv ? $"{adv.Name} L{a.Level}" : null)
            .Where(n => n is not null)
            .ToList();
        return named.Count > 0 ? string.Join(" + ", named) : null;
    }

    /// <summary>The Spirit Island wiki page for a catalogue entity, by its display name.</summary>
    public static string WikiLink(string name)
    {
        var slug = name.Replace("'", "%27").Replace(" ", "_");
        return $"https://spiritislandwiki.com/index.php?title={slug}";
    }
}
