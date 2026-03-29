namespace Domain.Models.Static.Data;

public static class Aspects
{
    public static IReadOnlyList<Aspect> All { get; } =
    [
        // Jagged Earth Aspects
        new(new("lightning-pandemonium"), "Pandemonium", Spirits.LightningsSwiftStrike, Expansions.JaggedEarth),
        new(new("lightning-wind"), "Wind", Spirits.LightningsSwiftStrike, Expansions.JaggedEarth),
        new(new("river-sunshine"), "Sunshine", Spirits.RiverSurgesInSunlight, Expansions.JaggedEarth),
        new(new("shadows-madness"), "Madness", Spirits.ShadowsFlickerLikeFlame, Expansions.JaggedEarth),
        new(new("shadows-reach"), "Reach", Spirits.ShadowsFlickerLikeFlame, Expansions.JaggedEarth),
        new(new("earth-resilience"), "Resilience", Spirits.VitalStrengthOfTheEarth, Expansions.JaggedEarth),

        // Feather & Flame Aspects
        new(new("lightning-immense"), "Immense", Spirits.LightningsSwiftStrike, Expansions.FeatherAndFlame),
        new(new("river-travel"), "Travel", Spirits.RiverSurgesInSunlight, Expansions.FeatherAndFlame),
        new(new("shadows-amorphous"), "Amorphous", Spirits.ShadowsFlickerLikeFlame, Expansions.FeatherAndFlame),
        new(new("shadows-foreboding"), "Foreboding", Spirits.ShadowsFlickerLikeFlame, Expansions.FeatherAndFlame),
        new(new("earth-might"), "Might", Spirits.VitalStrengthOfTheEarth, Expansions.FeatherAndFlame),

        // Nature Incarnate Aspects
        new(new("green-regrowth"), "Regrowth", Spirits.ASpreadOfRampantGreen, Expansions.NatureIncarnate),
        new(new("green-tangles"), "Tangles", Spirits.ASpreadOfRampantGreen, Expansions.NatureIncarnate),
        new(new("bringer-enticing"), "Enticing", Spirits.BringerOfDreamsAndNightmares, Expansions.NatureIncarnate),
        new(new("bringer-violence"), "Violence", Spirits.BringerOfDreamsAndNightmares, Expansions.NatureIncarnate),
        new(new("wildfire-transforming"), "Transforming", Spirits.HeartOfTheWildfire, Expansions.NatureIncarnate),
        new(new("keeper-spreading-hostility"), "Spreading Hostility", Spirits.KeeperOfTheForbiddenWilds, Expansions.NatureIncarnate),
        new(new("lightning-sparking"), "Sparking", Spirits.LightningsSwiftStrike, Expansions.NatureIncarnate),
        new(new("lure-lair"), "Lair", Spirits.LureOfTheDeepWilderness, Expansions.NatureIncarnate),
        new(new("ocean-deeps"), "Deeps", Spirits.OceansHungryGrasp, Expansions.NatureIncarnate),
        new(new("river-haven"), "Haven", Spirits.RiverSurgesInSunlight, Expansions.NatureIncarnate),
        new(new("serpent-locus"), "Locus", Spirits.SerpentSlumberingBeneathTheIsland, Expansions.NatureIncarnate),
        new(new("shadows-dark-fire"), "Dark Fire", Spirits.ShadowsFlickerLikeFlame, Expansions.NatureIncarnate),
        new(new("fangs-encircle"), "Encircle", Spirits.SharpFangsBehindTheLeaves, Expansions.NatureIncarnate),
        new(new("fangs-unconstrained"), "Unconstrained", Spirits.SharpFangsBehindTheLeaves, Expansions.NatureIncarnate),
        new(new("memory-intensify"), "Intensify", Spirits.ShiftingMemoryOfAges, Expansions.NatureIncarnate),
        new(new("memory-mentor"), "Mentor", Spirits.ShiftingMemoryOfAges, Expansions.NatureIncarnate),
        new(new("mist-stranded"), "Stranded", Spirits.ShroudOfSilentMist, Expansions.NatureIncarnate),
        new(new("thunderspeaker-tactician"), "Tactician", Spirits.Thunderspeaker, Expansions.NatureIncarnate),
        new(new("thunderspeaker-warrior"), "Warrior", Spirits.Thunderspeaker, Expansions.NatureIncarnate),
        new(new("earth-nourishing"), "Nourishing", Spirits.VitalStrengthOfTheEarth, Expansions.NatureIncarnate),
    ];
}
