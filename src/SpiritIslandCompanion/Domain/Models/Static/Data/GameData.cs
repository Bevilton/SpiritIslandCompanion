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

    public static IReadOnlyList<Spirit> GetSpiritsForExpansions(IEnumerable<ExpansionId> ownedExpansions)
    {
        var owned = ownedExpansions.ToHashSet();
        return Spirits.Where(s => owned.Contains(s.ExpansionId)).ToList();
    }

    public static IReadOnlyList<Aspect> GetAspectsForExpansions(IEnumerable<ExpansionId> ownedExpansions)
    {
        var owned = ownedExpansions.ToHashSet();
        return Aspects.Where(a => owned.Contains(a.ExpansionId)).ToList();
    }

    public static IReadOnlyList<Adversary> GetAdversariesForExpansions(IEnumerable<ExpansionId> ownedExpansions)
    {
        var owned = ownedExpansions.ToHashSet();
        return Adversaries.Where(a => owned.Contains(a.ExpansionId)).ToList();
    }

    public static IReadOnlyList<Scenario> GetScenariosForExpansions(IEnumerable<ExpansionId> ownedExpansions)
    {
        var owned = ownedExpansions.ToHashSet();
        return Scenarios.Where(s => owned.Contains(s.ExpansionId)).ToList();
    }

    public static IReadOnlyList<Board> GetBoardsForExpansions(IEnumerable<ExpansionId> ownedExpansions)
    {
        var owned = ownedExpansions.ToHashSet();
        return Boards.Where(b => owned.Contains(b.ExpansionId)).ToList();
    }

    public static IReadOnlyList<Aspect> GetAspectsForSpirit(SpiritId spiritId)
    {
        return Aspects.Where(a => a.SpiritId == spiritId).ToList();
    }
}
