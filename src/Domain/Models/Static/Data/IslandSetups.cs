namespace Domain.Models.Static.Data;

public static class IslandSetups
{
    public static IReadOnlyList<IslandSetup> All { get; } =
    [
        // Base Game layouts
        new(new("standard-1p"), "Standard", 1),
        new(new("standard-2p"), "Standard", 2),
        new(new("coastline-2p"), "Coastline", 2),
        new(new("standard-3p"), "Standard", 3),
        new(new("standard-4p"), "Standard", 4),

        // Jagged Earth layouts
        new(new("fragment-2p"), "Fragment", 2),
        new(new("opposite-shores-2p"), "Opposite Shores", 2),
        new(new("coastline-3p"), "Coastline", 3),
        new(new("sunrise-3p"), "Sunrise", 3),
        new(new("leaf-4p"), "Leaf", 4),
        new(new("snake-4p"), "Snake", 4),
        new(new("crab-5p"), "Crab", 5),
        new(new("claw-5p"), "Claw", 5),
        new(new("peninsula-5p"), "Peninsula", 5),
        new(new("snail-5p"), "Snail", 5),
        new(new("v-5p"), "V", 5),
        new(new("two-centers-6p"), "Two Centers", 6),
        new(new("caldera-6p"), "Caldera", 6),
        new(new("flower-6p"), "Flower", 6),
        new(new("star-6p"), "Star", 6),

        // Thematic layouts (no 5p — thematic island shape doesn't accommodate it)
        new(new("thematic-1p"), "Thematic", 1, isThematic: true),
        new(new("thematic-2p"), "Thematic", 2, isThematic: true),
        new(new("thematic-3p"), "Thematic", 3, isThematic: true),
        new(new("thematic-4p"), "Thematic", 4, isThematic: true),
        new(new("thematic-6p"), "Thematic", 6, isThematic: true),

        // Hand-built in the island playground. These record only the board count — the
        // arrangement itself is stored on the game, and named layouts the player wants to
        // reuse live in CustomIslandLayout.
        new(new("custom-1p"), "Custom", 1, isCustom: true),
        new(new("custom-2p"), "Custom", 2, isCustom: true),
        new(new("custom-3p"), "Custom", 3, isCustom: true),
        new(new("custom-4p"), "Custom", 4, isCustom: true),
        new(new("custom-5p"), "Custom", 5, isCustom: true),
        new(new("custom-6p"), "Custom", 6, isCustom: true),
    ];

    /// <summary>
    /// The layouts that exist as shapes — everything except the hand-built placeholders, which
    /// name a board count and nothing else. Anything that offers a layout to look at or choose
    /// from wants this rather than <see cref="All"/>: a custom id has no arrangement of its own,
    /// so there is no diagram to draw and picking one without also supplying the geometry is
    /// rejected (see <c>GameFactory.BuildIslandAsync</c>).
    /// </summary>
    public static IReadOnlyList<IslandSetup> Published { get; } = All.Where(s => !s.IsCustom).ToList();

    /// The id a hand-built arrangement of <paramref name="boardCount"/> boards is saved under.
    public static string CustomIdFor(int boardCount) => $"custom-{boardCount}p";

    public static bool IsCustomId(string? id) => id is not null && id.StartsWith("custom-", StringComparison.Ordinal);

    /// <summary>
    /// The thematic island for <paramref name="boardCount"/> boards, or null when there
    /// isn't one. Thematic maps are a fixed island cut into a fixed number of pieces, so
    /// there is exactly one arrangement per supported count — and none at all for five,
    /// which the real island's shape cannot be divided into.
    /// </summary>
    public static IslandSetup? ThematicFor(int boardCount) =>
        All.FirstOrDefault(s => s.IsThematic && s.NumberOfPlayers == boardCount);
}
