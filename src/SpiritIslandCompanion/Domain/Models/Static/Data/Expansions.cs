namespace Domain.Models.Static.Data;

public static class Expansions
{
    public static readonly ExpansionId BaseGame = new("base-game");
    public static readonly ExpansionId BranchAndClaw = new("branch-and-claw");
    public static readonly ExpansionId JaggedEarth = new("jagged-earth");
    public static readonly ExpansionId FeatherAndFlame = new("feather-and-flame");
    public static readonly ExpansionId HorizonsOfSpiritIsland = new("horizons");
    public static readonly ExpansionId NatureIncarnate = new("nature-incarnate");

    public static IReadOnlyList<Expansion> All { get; } =
    [
        new(BaseGame, "Spirit Island"),
        new(BranchAndClaw, "Branch & Claw"),
        new(JaggedEarth, "Jagged Earth"),
        new(FeatherAndFlame, "Feather & Flame"),
        new(HorizonsOfSpiritIsland, "Horizons of Spirit Island"),
        new(NatureIncarnate, "Nature Incarnate"),
    ];
}
