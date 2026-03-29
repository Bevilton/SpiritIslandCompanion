namespace Domain.Models.Static.Data;

public static class Adversaries
{
    public static readonly AdversaryId BrandenburgPrussia = new("brandenburg-prussia");
    public static readonly AdversaryId England = new("england");
    public static readonly AdversaryId Sweden = new("sweden");
    public static readonly AdversaryId France = new("france");
    public static readonly AdversaryId HabsburgMonarchy = new("habsburg-monarchy");
    public static readonly AdversaryId Russia = new("russia");
    public static readonly AdversaryId Scotland = new("scotland");
    public static readonly AdversaryId HabsburgMining = new("habsburg-mining");

    public static IReadOnlyList<Adversary> All { get; } =
    [
        // Base Game
        new(BrandenburgPrussia, "Brandenburg-Prussia", Expansions.BaseGame,
        [
            new(0, 1), new(1, 2), new(2, 4), new(3, 6), new(4, 7), new(5, 9), new(6, 10)
        ]),
        new(England, "England", Expansions.BaseGame,
        [
            new(0, 1), new(1, 3), new(2, 4), new(3, 6), new(4, 7), new(5, 9), new(6, 11)
        ]),
        new(Sweden, "Sweden", Expansions.BaseGame,
        [
            new(0, 1), new(1, 2), new(2, 3), new(3, 5), new(4, 6), new(5, 7), new(6, 8)
        ]),

        // Branch & Claw
        new(France, "France (Plantation Colony)", Expansions.BranchAndClaw,
        [
            new(0, 2), new(1, 3), new(2, 5), new(3, 7), new(4, 8), new(5, 9), new(6, 10)
        ]),

        // Jagged Earth
        new(HabsburgMonarchy, "Habsburg Monarchy (Livestock Colony)", Expansions.JaggedEarth,
        [
            new(0, 2), new(1, 3), new(2, 5), new(3, 6), new(4, 8), new(5, 9), new(6, 10)
        ]),
        new(Russia, "Russia", Expansions.JaggedEarth,
        [
            new(0, 1), new(1, 3), new(2, 4), new(3, 6), new(4, 7), new(5, 9), new(6, 11)
        ]),

        // Feather & Flame
        new(Scotland, "Scotland", Expansions.FeatherAndFlame,
        [
            new(0, 1), new(1, 3), new(2, 4), new(3, 6), new(4, 7), new(5, 8), new(6, 10)
        ]),

        // Nature Incarnate
        new(HabsburgMining, "Habsburg Mining Expedition", Expansions.NatureIncarnate,
        [
            new(0, 1), new(1, 3), new(2, 4), new(3, 5), new(4, 7), new(5, 9), new(6, 10)
        ]),
    ];
}
