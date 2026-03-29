namespace Domain.Models.Static.Data;

public static class Spirits
{
    // Base Game
    public static readonly SpiritId LightningsSwiftStrike = new("lightnings-swift-strike");
    public static readonly SpiritId RiverSurgesInSunlight = new("river-surges-in-sunlight");
    public static readonly SpiritId ShadowsFlickerLikeFlame = new("shadows-flicker-like-flame");
    public static readonly SpiritId VitalStrengthOfTheEarth = new("vital-strength-of-the-earth");
    public static readonly SpiritId ASpreadOfRampantGreen = new("a-spread-of-rampant-green");
    public static readonly SpiritId Thunderspeaker = new("thunderspeaker");
    public static readonly SpiritId BringerOfDreamsAndNightmares = new("bringer-of-dreams-and-nightmares");
    public static readonly SpiritId OceansHungryGrasp = new("oceans-hungry-grasp");

    // Branch & Claw
    public static readonly SpiritId KeeperOfTheForbiddenWilds = new("keeper-of-the-forbidden-wilds");
    public static readonly SpiritId SharpFangsBehindTheLeaves = new("sharp-fangs-behind-the-leaves");

    // Feather & Flame (formerly Promo Pack 1 + 2)
    public static readonly SpiritId HeartOfTheWildfire = new("heart-of-the-wildfire");
    public static readonly SpiritId SerpentSlumberingBeneathTheIsland = new("serpent-slumbering-beneath-the-island");
    public static readonly SpiritId DownpourDrenchesTheWorld = new("downpour-drenches-the-world");
    public static readonly SpiritId FinderOfPathsUnseen = new("finder-of-paths-unseen");

    // Jagged Earth
    public static readonly SpiritId GrinningTricksterStirsUpTrouble = new("grinning-trickster-stirs-up-trouble");
    public static readonly SpiritId LureOfTheDeepWilderness = new("lure-of-the-deep-wilderness");
    public static readonly SpiritId ManyMindsMoveAsOne = new("many-minds-move-as-one");
    public static readonly SpiritId ShiftingMemoryOfAges = new("shifting-memory-of-ages");
    public static readonly SpiritId StonesUnyieldingDefiance = new("stones-unyielding-defiance");
    public static readonly SpiritId VolcanoLoomingHigh = new("volcano-looming-high");
    public static readonly SpiritId ShroudOfSilentMist = new("shroud-of-silent-mist");
    public static readonly SpiritId VengeanceAsABurningPlague = new("vengeance-as-a-burning-plague");
    public static readonly SpiritId FracturedDaysSplitTheSky = new("fractured-days-split-the-sky");
    public static readonly SpiritId StarlightSeeksItsForm = new("starlight-seeks-its-form");

    // Horizons of Spirit Island
    public static readonly SpiritId DevouringTeethLurkUnderfoot = new("devouring-teeth-lurk-underfoot");
    public static readonly SpiritId EyesWatchFromTheTrees = new("eyes-watch-from-the-trees");
    public static readonly SpiritId FathomlessMudOfTheSwamp = new("fathomless-mud-of-the-swamp");
    public static readonly SpiritId RisingHeatOfStoneAndSand = new("rising-heat-of-stone-and-sand");
    public static readonly SpiritId SunBrightWhirlwind = new("sun-bright-whirlwind");

    // Nature Incarnate
    public static readonly SpiritId EmberEyedBehemoth = new("ember-eyed-behemoth");
    public static readonly SpiritId HearthVigil = new("hearth-vigil");
    public static readonly SpiritId ToweringRootsOfTheJungle = new("towering-roots-of-the-jungle");
    public static readonly SpiritId BreathOfDarknessDownYourSpine = new("breath-of-darkness-down-your-spine");
    public static readonly SpiritId RelentlessGazeOfTheSun = new("relentless-gaze-of-the-sun");
    public static readonly SpiritId WanderingVoiceKeensDelirium = new("wandering-voice-keens-delirium");
    public static readonly SpiritId WoundedWatersBleeding = new("wounded-waters-bleeding");
    public static readonly SpiritId DancesUpEarthquakes = new("dances-up-earthquakes");

    public static IReadOnlyList<Spirit> All { get; } =
    [
        // Base Game
        new(LightningsSwiftStrike, "Lightning's Swift Strike", Expansions.BaseGame),
        new(RiverSurgesInSunlight, "River Surges in Sunlight", Expansions.BaseGame),
        new(ShadowsFlickerLikeFlame, "Shadows Flicker Like Flame", Expansions.BaseGame),
        new(VitalStrengthOfTheEarth, "Vital Strength of the Earth", Expansions.BaseGame),
        new(ASpreadOfRampantGreen, "A Spread of Rampant Green", Expansions.BaseGame),
        new(Thunderspeaker, "Thunderspeaker", Expansions.BaseGame),
        new(BringerOfDreamsAndNightmares, "Bringer of Dreams and Nightmares", Expansions.BaseGame),
        new(OceansHungryGrasp, "Ocean's Hungry Grasp", Expansions.BaseGame),

        // Branch & Claw
        new(KeeperOfTheForbiddenWilds, "Keeper of the Forbidden Wilds", Expansions.BranchAndClaw),
        new(SharpFangsBehindTheLeaves, "Sharp Fangs Behind the Leaves", Expansions.BranchAndClaw),

        // Feather & Flame
        new(HeartOfTheWildfire, "Heart of the Wildfire", Expansions.FeatherAndFlame),
        new(SerpentSlumberingBeneathTheIsland, "Serpent Slumbering Beneath the Island", Expansions.FeatherAndFlame),
        new(DownpourDrenchesTheWorld, "Downpour Drenches the World", Expansions.FeatherAndFlame),
        new(FinderOfPathsUnseen, "Finder of Paths Unseen", Expansions.FeatherAndFlame),

        // Jagged Earth
        new(GrinningTricksterStirsUpTrouble, "Grinning Trickster Stirs Up Trouble", Expansions.JaggedEarth),
        new(LureOfTheDeepWilderness, "Lure of the Deep Wilderness", Expansions.JaggedEarth),
        new(ManyMindsMoveAsOne, "Many Minds Move as One", Expansions.JaggedEarth),
        new(ShiftingMemoryOfAges, "Shifting Memory of Ages", Expansions.JaggedEarth),
        new(StonesUnyieldingDefiance, "Stone's Unyielding Defiance", Expansions.JaggedEarth),
        new(VolcanoLoomingHigh, "Volcano Looming High", Expansions.JaggedEarth),
        new(ShroudOfSilentMist, "Shroud of Silent Mist", Expansions.JaggedEarth),
        new(VengeanceAsABurningPlague, "Vengeance as a Burning Plague", Expansions.JaggedEarth),
        new(FracturedDaysSplitTheSky, "Fractured Days Split the Sky", Expansions.JaggedEarth),
        new(StarlightSeeksItsForm, "Starlight Seeks Its Form", Expansions.JaggedEarth),

        // Horizons of Spirit Island
        new(DevouringTeethLurkUnderfoot, "Devouring Teeth Lurk Underfoot", Expansions.HorizonsOfSpiritIsland),
        new(EyesWatchFromTheTrees, "Eyes Watch from the Trees", Expansions.HorizonsOfSpiritIsland),
        new(FathomlessMudOfTheSwamp, "Fathomless Mud of the Swamp", Expansions.HorizonsOfSpiritIsland),
        new(RisingHeatOfStoneAndSand, "Rising Heat of Stone and Sand", Expansions.HorizonsOfSpiritIsland),
        new(SunBrightWhirlwind, "Sun-Bright Whirlwind", Expansions.HorizonsOfSpiritIsland),

        // Nature Incarnate
        new(EmberEyedBehemoth, "Ember-Eyed Behemoth", Expansions.NatureIncarnate),
        new(HearthVigil, "Hearth-Vigil", Expansions.NatureIncarnate),
        new(ToweringRootsOfTheJungle, "Towering Roots of the Jungle", Expansions.NatureIncarnate),
        new(BreathOfDarknessDownYourSpine, "Breath of Darkness Down Your Spine", Expansions.NatureIncarnate),
        new(RelentlessGazeOfTheSun, "Relentless Gaze of the Sun", Expansions.NatureIncarnate),
        new(WanderingVoiceKeensDelirium, "Wandering Voice Keens Delirium", Expansions.NatureIncarnate),
        new(WoundedWatersBleeding, "Wounded Waters Bleeding", Expansions.NatureIncarnate),
        new(DancesUpEarthquakes, "Dances Up Earthquakes", Expansions.NatureIncarnate),
    ];
}
