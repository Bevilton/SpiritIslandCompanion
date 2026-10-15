namespace Domain.Models.Static.Data;

public static class GameData
{
    public static IReadOnlyList<Expansion> Expansions => Static.Data.Expansions.All;
    public static IReadOnlyList<Spirit> Spirits => Static.Data.Spirits.All;
    public static IReadOnlyList<Aspect> Aspects => Static.Data.Aspects.All;
    public static IReadOnlyList<Adversary> Adversaries => Static.Data.Adversaries.All;
    public static IReadOnlyList<Scenario> Scenarios => Static.Data.Scenarios.All;
    public static IReadOnlyList<Board> Boards => Static.Data.Boards.All;
    public static IReadOnlyList<IslandSetup> IslandSetups => Static.Data.IslandSetups.All;

    /// <summary>
    /// Island setups that are an actual arrangement — use this to show or offer layouts, and
    /// <see cref="IslandSetups"/> only to resolve an id that has already been recorded. See
    /// <see cref="Static.Data.IslandSetups.Published"/>.
    /// </summary>
    public static IReadOnlyList<IslandSetup> PublishedIslandSetups => Static.Data.IslandSetups.Published;

    // Deliberately no "for these expansions" helpers here: narrowing a catalogue to a reader's
    // shelf is the presentation layer's question, and its answer depends on what an unknown
    // shelf means there ("show everything", not the empty set). The domain lists the catalogue;
    // who owns what is decided above it.

    public static IReadOnlyList<Aspect> GetAspectsForSpirit(SpiritId spiritId) =>
        Aspects.Where(a => a.SpiritId == spiritId).ToList();
}
