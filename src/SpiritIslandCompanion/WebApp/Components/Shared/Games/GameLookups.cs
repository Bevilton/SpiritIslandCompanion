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
}
