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
    /// <summary>Fallback swatch for an entity without a catalogue colour of its own.</summary>
    public const string NeutralColor = "#78716c";

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

    /// <summary>The Spirit Island wiki page for a catalogue entity, by its display name.</summary>
    public static string WikiLink(string name)
    {
        var slug = name.Replace("'", "%27").Replace(" ", "_");
        return $"https://spiritislandwiki.com/index.php?title={slug}";
    }
}
