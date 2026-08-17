namespace Domain.Models.Static.Data;

public static class Scenarios
{
    public static IReadOnlyList<Scenario> All { get; } =
    [
        // Base Game
        new(new("blitz"), "Blitz", 0, Expansions.BaseGame),
        new(new("guard-the-isles-heart"), "Guard the Isle's Heart", 0, Expansions.BaseGame),
        new(new("rituals-of-terror"), "Rituals of Terror", 3, Expansions.BaseGame),

        // Branch & Claw
        new(new("second-wave"), "Second Wave", 1, Expansions.BranchAndClaw),
        new(new("powers-long-forgotten"), "Powers Long Forgotten", 1, Expansions.BranchAndClaw),
        new(new("ward-the-shores"), "Ward the Shores", 2, Expansions.BranchAndClaw),
        new(new("rituals-of-the-destroying-flame"), "Rituals of the Destroying Flame", 3, Expansions.BranchAndClaw),
        new(new("dahan-insurrection"), "Dahan Insurrection", 4, Expansions.BranchAndClaw),

        // Jagged Earth
        new(new("elemental-invocation"), "Elemental Invocation", 1, Expansions.JaggedEarth),
        new(new("despicable-theft"), "Despicable Theft", 2, Expansions.JaggedEarth),
        new(new("the-great-river"), "The Great River", 3, Expansions.JaggedEarth),

        // Feather & Flame
        new(new("a-diversity-of-spirits"), "A Diversity of Spirits", 0, Expansions.FeatherAndFlame),
        new(new("varied-terrains"), "Varied Terrains", 2, Expansions.FeatherAndFlame),

        // Nature Incarnate
        new(new("destiny-unfolds"), "Destiny Unfolds", 0, Expansions.NatureIncarnate),
        new(new("surges-of-colonization"), "Surges of Colonization", 2, Expansions.NatureIncarnate),
    ];
}
