namespace Domain.Models.Static;

public class IslandSetup
{
    public IslandSetupId Id { get; init; }
    public string Name { get; init; }
    public int NumberOfPlayers { get; init; }
    public bool IsThematic { get; init; }

    /// <summary>
    /// A layout the player arranged by hand in the island playground rather than one of
    /// the published shapes. There is one per board count, and it carries no geometry of
    /// its own — the arrangement is stored on the game (see <c>Game.IslandLayout</c>).
    /// Never valid with thematic maps, which are a fixed island with nothing to arrange.
    /// </summary>
    public bool IsCustom { get; init; }

    public IslandSetup(IslandSetupId id, string name, int numberOfPlayers, bool isThematic = false, bool isCustom = false)
    {
        Id = id;
        Name = name;
        NumberOfPlayers = numberOfPlayers;
        IsThematic = isThematic;
        IsCustom = isCustom;
    }
}
