using Application.Features.Games.Dtos;
using Domain.Models.Static;
using Domain.Models.Static.Data;

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

    /// <summary>The fallback <see cref="MatchTitle"/>, for callers that style it differently.</summary>
    public const string FreeplayTitle = "Tutorial / freeplay";

    /// <summary>
    /// What a recorded game is called wherever one is listed: the adversaries fought, or the
    /// scenario when there were none, or "Tutorial / freeplay" when there was neither. One
    /// helper so the games list, the game detail and anything else that names a match agree.
    /// </summary>
    public static string MatchTitle(IReadOnlyList<string> adversaryIds, string? scenarioId)
    {
        var adversaries = adversaryIds
            .Select(id => AdversaryFor(id)?.Name)
            .Where(n => n is not null)
            .ToList();
        if (adversaries.Count > 0) return string.Join(" + ", adversaries);
        return ScenarioFor(scenarioId)?.Name ?? FreeplayTitle;
    }

    /// <summary>
    /// <see cref="MatchTitle(IReadOnlyList{string}, string?)"/> with the levels fought, for
    /// callers whose data carries them: "England L3 + Russia L2".
    /// </summary>
    public static string MatchTitle(IReadOnlyList<GameAdversaryResponse> adversaries, string? scenarioId) =>
        AdversaryLabel(adversaries) ?? ScenarioFor(scenarioId)?.Name ?? FreeplayTitle;

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
