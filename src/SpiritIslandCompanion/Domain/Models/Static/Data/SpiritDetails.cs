namespace Domain.Models.Static.Data;

public enum Complexity { Low, Moderate, High, VeryHigh }

public enum Element { Sun, Moon, Fire, Air, Water, Earth, Plant, Animal }

public enum Token { Dahan, Beasts, Wilds, Disease, Strife, Badlands }

public sealed record SpiritDetail(
    string ColorHex,
    Complexity Complexity,
    string PlayStyle,
    string Summary,
    IReadOnlyList<Element> Elements,
    IReadOnlyList<Token> Tokens);

public sealed record AspectDetail(string Summary);

/// <summary>
/// Static enrichment data for spirits and aspects (color, complexity, short
/// flavour text, primary thematic elements). Sourced from the Spirit Island
/// wiki at spiritislandwiki.com.
/// </summary>
public static class SpiritDetails
{
    public static IReadOnlyDictionary<SpiritId, SpiritDetail> All { get; } = new Dictionary<SpiritId, SpiritDetail>
    {
        // ---------- Base Game ----------
        [Spirits.LightningsSwiftStrike] = new(
            "#E6C341", Complexity.Low,
            "Fast offense",
            "Strikes fast and hard with flashes of cheap, low-cost power. Excels at attacking invaders before they can build, and at finishing battles quickly.",
            [Element.Air, Element.Fire],
            []),

        [Spirits.RiverSurgesInSunlight] = new(
            "#4FB6D9", Complexity.Low,
            "Pushing & gathering",
            "Generous and flowing, the River pushes invaders away from coastal lands and gathers Dahan to defend. Reliable energy and many element-rich powers.",
            [Element.Sun, Element.Water, Element.Earth],
            [Token.Dahan]),

        [Spirits.ShadowsFlickerLikeFlame] = new(
            "#6B4F9B", Complexity.Low,
            "Stealth & fear",
            "Hides among the trees and ruins, generating fear and slipping unseen. Strong on tight maps and against adversaries that punish revealed presence.",
            [Element.Moon, Element.Fire],
            [Token.Strife]),

        [Spirits.VitalStrengthOfTheEarth] = new(
            "#8B6F47", Complexity.Low,
            "Slow & defensive",
            "Steady, powerful, and tough. Defends the island with high-impact powers and shrugs off blight thanks to deep reserves of energy and presence.",
            [Element.Earth, Element.Plant],
            [Token.Dahan]),

        [Spirits.ASpreadOfRampantGreen] = new(
            "#5BAE5B", Complexity.Moderate,
            "Sacred sites & disease",
            "Reclaims the island through entangling vegetation and creeping disease, building dense webs of sacred sites that lock invaders in place.",
            [Element.Plant, Element.Water],
            [Token.Disease]),

        [Spirits.Thunderspeaker] = new(
            "#E0A04E", Complexity.Moderate,
            "Dahan warband",
            "Rallies and leads the Dahan into open battle, marching them across the island to strike together. Vulnerable when the Dahan thin out.",
            [Element.Sun, Element.Fire, Element.Air],
            [Token.Dahan]),

        [Spirits.BringerOfDreamsAndNightmares] = new(
            "#3D2D6B", Complexity.Moderate,
            "Pure fear",
            "Wins almost entirely through fear — driving invaders to despair and flight rather than smashing them outright. Plays unlike any other spirit.",
            [Element.Moon, Element.Air],
            [Token.Strife]),

        [Spirits.OceansHungryGrasp] = new(
            "#1A5F8A", Complexity.High,
            "Coastal devourer",
            "Anchors itself in the Ocean and drowns invaders, towns, and cities at the coast. Restricted home but devastating in shoreline waves.",
            [Element.Water, Element.Moon],
            []),

        // ---------- Branch & Claw ----------
        [Spirits.KeeperOfTheForbiddenWilds] = new(
            "#3B7A47", Complexity.Moderate,
            "Wilds & jungle dominion",
            "Guardian of jungle and wetland, the Keeper grows wilds that smother explorers and starve invader expansion.",
            [Element.Sun, Element.Plant],
            [Token.Wilds]),

        [Spirits.SharpFangsBehindTheLeaves] = new(
            "#A0392B", Complexity.Moderate,
            "Beasts on the hunt",
            "Forms hunting packs of beasts that ambush invaders deep in the jungle. Strong synergy with Branch & Claw events.",
            [Element.Plant, Element.Animal],
            [Token.Beasts]),

        // ---------- Feather & Flame ----------
        [Spirits.HeartOfTheWildfire] = new(
            "#D9542B", Complexity.High,
            "Self-blight engine",
            "Burns the island to save it — feeds blight to itself for fuel, then unleashes scorching damage. Demands tight blight management.",
            [Element.Fire, Element.Air],
            [Token.Badlands]),

        [Spirits.SerpentSlumberingBeneathTheIsland] = new(
            "#2D8276", Complexity.VeryHigh,
            "Slow buildup, late explosion",
            "Sleeps for the early game, devouring presence and energy. Wakes to terrifying mid- and late-game power once enough has been consumed.",
            [Element.Earth, Element.Water, Element.Fire],
            []),

        [Spirits.DownpourDrenchesTheWorld] = new(
            "#4A7DA0", Complexity.Moderate,
            "Floods & rain",
            "Drowns the lowlands with relentless rain, forcing invaders into ever-shrinking high ground.",
            [Element.Water, Element.Air],
            []),

        [Spirits.FinderOfPathsUnseen] = new(
            "#8B7DB8", Complexity.High,
            "Mobility & isolation",
            "Walks hidden paths and pulls allies along them, isolating invaders one at a time and granting partners extra movement and reach.",
            [Element.Air, Element.Moon],
            []),

        // ---------- Jagged Earth ----------
        [Spirits.GrinningTricksterStirsUpTrouble] = new(
            "#B8487A", Complexity.Moderate,
            "Mischief & infighting",
            "Spreads chaos and turns invaders against each other. Pranks, fear, and unpredictable powers across the island.",
            [Element.Moon, Element.Fire, Element.Air],
            [Token.Strife]),

        [Spirits.LureOfTheDeepWilderness] = new(
            "#2D5C3D", Complexity.High,
            "Drawing & isolation",
            "Beckons invaders ever deeper into the wild and consumes them where help cannot follow.",
            [Element.Moon, Element.Plant],
            [Token.Wilds]),

        [Spirits.ManyMindsMoveAsOne] = new(
            "#9B6B3D", Complexity.High,
            "Beast swarm",
            "A flock with no single body — many beast presences sweep across lands together, hunting in coordinated waves.",
            [Element.Water, Element.Animal],
            [Token.Beasts]),

        [Spirits.ShiftingMemoryOfAges] = new(
            "#B8A074", Complexity.High,
            "Element manipulation",
            "An ancient archive that shifts elements at will, supports allies, and rewards careful long-term planning.",
            [Element.Sun, Element.Air, Element.Earth],
            []),

        [Spirits.StonesUnyieldingDefiance] = new(
            "#7A6E5C", Complexity.Moderate,
            "Rock-solid defense",
            "Refuses to yield — armours lands against blight and damage, and turns the invaders' force back upon themselves.",
            [Element.Earth, Element.Fire],
            [Token.Badlands]),

        [Spirits.VolcanoLoomingHigh] = new(
            "#D9622B", Complexity.High,
            "Eruption damage",
            "Builds pressure across the highlands, then erupts in catastrophic damage that ripples across nearby lands.",
            [Element.Fire, Element.Earth],
            [Token.Badlands]),

        [Spirits.ShroudOfSilentMist] = new(
            "#9DA3A8", Complexity.High,
            "Soft removal",
            "Wraps the island in silent fog, dissolving invaders quietly and erasing presence rather than smashing it.",
            [Element.Moon, Element.Air, Element.Water],
            []),

        [Spirits.VengeanceAsABurningPlague] = new(
            "#B8302E", Complexity.Moderate,
            "Retaliation",
            "Punishes invaders for every Dahan they harm, building damaging reprisal where blood has been spilled.",
            [Element.Fire, Element.Animal],
            [Token.Dahan, Token.Disease]),

        [Spirits.FracturedDaysSplitTheSky] = new(
            "#6BB8C7", Complexity.VeryHigh,
            "Time manipulation",
            "Shatters the flow of time, replaying turns and rearranging events. The most mechanically demanding spirit in the game.",
            [Element.Sun, Element.Air, Element.Moon],
            []),

        [Spirits.StarlightSeeksItsForm] = new(
            "#6B6BB8", Complexity.VeryHigh,
            "Customisation",
            "Begins formless and grows by acquiring innate traits and special rules — every game plays differently.",
            [Element.Air, Element.Moon],
            []),

        // ---------- Horizons of Spirit Island (entry-level) ----------
        [Spirits.DevouringTeethLurkUnderfoot] = new(
            "#6B4424", Complexity.Low,
            "Direct destruction",
            "Beasts emerge from the dark earth to swallow invaders whole. Blunt, satisfying, and easy to learn.",
            [Element.Earth, Element.Animal],
            [Token.Beasts]),

        [Spirits.EyesWatchFromTheTrees] = new(
            "#4A6B3D", Complexity.Low,
            "Sacred-site fear",
            "Watching presences in the canopy spread quiet dread; the more sacred sites, the deeper the fear.",
            [Element.Plant, Element.Moon],
            []),

        [Spirits.FathomlessMudOfTheSwamp] = new(
            "#6B6B3D", Complexity.Low,
            "Wetland blight defence",
            "Sinks invaders in heavy mud and shrugs off blight in soft wetland terrain.",
            [Element.Earth, Element.Water],
            []),

        [Spirits.RisingHeatOfStoneAndSand] = new(
            "#C77E47", Complexity.Low,
            "Push & burn",
            "Bakes lowlands and pushes towns and cities back into the hot sand to wither.",
            [Element.Sun, Element.Fire],
            [Token.Badlands]),

        [Spirits.SunBrightWhirlwind] = new(
            "#E8B547", Complexity.Low,
            "Mobile striker",
            "Whirls across the island, scattering invaders with bursts of light and lifting allies into reach of distant lands.",
            [Element.Sun, Element.Air],
            []),

        // ---------- Nature Incarnate ----------
        [Spirits.EmberEyedBehemoth] = new(
            "#C7472B", Complexity.Moderate,
            "Roaming destroyer",
            "A massive moving presence that levels everything in its path. Plays around tracking and unleashing the Behemoth's location.",
            [Element.Fire, Element.Animal],
            [Token.Beasts, Token.Badlands]),

        [Spirits.HearthVigil] = new(
            "#D97D47", Complexity.Moderate,
            "Dahan protector",
            "Burns at the centre of every Dahan home, defending them ferociously and rewarding their gathering.",
            [Element.Fire, Element.Earth],
            [Token.Dahan]),

        [Spirits.ToweringRootsOfTheJungle] = new(
            "#3D6B47", Complexity.Moderate,
            "Wilds-control engine",
            "Roots vast networks below ground that strangle the spread of invaders across whole regions.",
            [Element.Earth, Element.Plant],
            [Token.Wilds]),

        [Spirits.BreathOfDarknessDownYourSpine] = new(
            "#4A2D6B", Complexity.High,
            "Fear & creeping dread",
            "An incarnated dread that builds ever-larger card and fear effects the more it terrifies.",
            [Element.Moon, Element.Animal],
            [Token.Beasts]),

        [Spirits.RelentlessGazeOfTheSun] = new(
            "#E8A547", Complexity.High,
            "Light & exposure",
            "A scorching, all-seeing eye that burns invaders openly and reveals what is hidden.",
            [Element.Sun, Element.Fire],
            []),

        [Spirits.WanderingVoiceKeensDelirium] = new(
            "#5C3D7A", Complexity.VeryHigh,
            "Songs of madness",
            "Sings songs whose verses change every game; weaves persistent effects across the island that build into delirium.",
            [Element.Sun, Element.Air, Element.Moon],
            [Token.Strife]),

        [Spirits.WoundedWatersBleeding] = new(
            "#7A2D4A", Complexity.High,
            "Healing through harm",
            "Bleeds and grows stronger from its own pain, channelling damage into recovery and reprisal.",
            [Element.Water, Element.Animal],
            [Token.Beasts]),

        [Spirits.DancesUpEarthquakes] = new(
            "#A0522D", Complexity.High,
            "Build-up & release",
            "Stomps a steady rhythm that builds tension across the land, then releases it in earth-shattering quakes.",
            [Element.Earth, Element.Animal, Element.Fire],
            [Token.Beasts, Token.Badlands]),
    };

    public static SpiritDetail? For(SpiritId id)
        => All.GetValueOrDefault(id);
}

public static class AspectDetails
{
    public static IReadOnlyDictionary<AspectId, AspectDetail> All { get; } = new Dictionary<AspectId, AspectDetail>
    {
        // Lightning's Swift Strike
        [new("lightning-pandemonium")] = new("Trades raw damage for chaos — drives invaders mad with fear-on-strike effects."),
        [new("lightning-wind")] = new("Adds movement and air-element support, pushing and repositioning more freely."),
        [new("lightning-immense")] = new("Scales up power: bigger, costlier strikes that level whole regions."),
        [new("lightning-sparking")] = new("Spreads small bursts of energy to allies, electrifying their plays."),

        // River Surges in Sunlight
        [new("river-sunshine")] = new("Brightens the island with extra sun, boosting healing and growth effects."),
        [new("river-travel")] = new("Lets the River carry allies and Dahan along its currents."),
        [new("river-haven")] = new("Turns wetlands into refuges that shelter Dahan and resist invaders."),

        // Shadows Flicker Like Flame
        [new("shadows-madness")] = new("Pushes harder on fear and terror, breaking invaders' resolve."),
        [new("shadows-reach")] = new("Extends Shadows' powers across longer distances on the map."),
        [new("shadows-amorphous")] = new("Slips between forms, swapping powers and presence with unusual flexibility."),
        [new("shadows-foreboding")] = new("Front-loads fear cards and dread, accelerating fear-based wins."),
        [new("shadows-dark-fire")] = new("Leans into fire damage, burning where shadows fall."),

        // Vital Strength of the Earth
        [new("earth-resilience")] = new("Even tougher and harder to dislodge — favours blight defence and endurance."),
        [new("earth-might")] = new("Hits harder up front, trading some recovery for raw force."),
        [new("earth-nourishing")] = new("Restores Dahan and nurtures the island, with stronger support and healing."),

        // A Spread of Rampant Green
        [new("green-regrowth")] = new("Grows back from setbacks faster, with more healing and recovery."),
        [new("green-tangles")] = new("Locks invaders down with sticky vegetation that punishes movement."),

        // Bringer of Dreams and Nightmares
        [new("bringer-enticing")] = new("Lures invaders into bad terrain, then hits them with terror."),
        [new("bringer-violence")] = new("Adds direct damage to fear, blending nightmare with bloodshed."),

        // Heart of the Wildfire
        [new("wildfire-transforming")] = new("Reshapes the relationship with blight — burns blight into power differently."),

        // Keeper of the Forbidden Wilds
        [new("keeper-spreading-hostility")] = new("Grows hostile wilds further, making the jungle openly attack invaders."),

        // Lure of the Deep Wilderness
        [new("lure-lair")] = new("Builds a dread lair deep in the wilds where invaders are torn apart."),

        // Ocean's Hungry Grasp
        [new("ocean-deeps")] = new("Pulls invaders into the depths, drowning them with greater range and certainty."),

        // Serpent Slumbering Beneath the Island
        [new("serpent-locus")] = new("Anchors the Serpent's growth around a central locus rather than spreading thin."),

        // Sharp Fangs Behind the Leaves
        [new("fangs-encircle")] = new("Surrounds invaders before striking, gaining position before damage."),
        [new("fangs-unconstrained")] = new("Lets the beasts roam freely with fewer restrictions on hunting."),

        // Shifting Memory of Ages
        [new("memory-intensify")] = new("Doubles down on element manipulation, ramping up element-based effects."),
        [new("memory-mentor")] = new("Teaches and supports allies more directly, sharing power and elements."),

        // Shroud of Silent Mist
        [new("mist-stranded")] = new("Strands invaders in fog where they cannot reach reinforcements."),

        // Thunderspeaker
        [new("thunderspeaker-tactician")] = new("Coordinates the Dahan with battlefield tactics and positioning."),
        [new("thunderspeaker-warrior")] = new("Joins the Dahan in melee, hitting harder alongside them."),
    };

    public static AspectDetail? For(AspectId id)
        => All.GetValueOrDefault(id);
}
