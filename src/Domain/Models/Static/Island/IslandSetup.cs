namespace Domain.Models.Static;

/// <param name="BoardCount">
/// How many boards the layout is cut for: one per player, plus the extra board when a game
/// adds one — so a four-board layout also serves a three-player table with an extra board.
/// </param>
/// <param name="IsCustom">
/// A layout the player arranged by hand in the island playground rather than one of
/// the published shapes. There is one per board count, and it carries no geometry of
/// its own — the arrangement is stored on the game (see <c>Game.IslandLayout</c>).
/// Never valid with thematic maps, which are a fixed island with nothing to arrange.
/// </param>
public sealed record IslandSetup(
    IslandSetupId Id,
    string Name,
    int BoardCount,
    bool IsThematic = false,
    bool IsCustom = false);
