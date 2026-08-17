namespace Domain.Models.Static.Data;

public sealed record ScenarioDetail(string Summary);

/// <summary>
/// Short flavour / theme summaries for scenarios. For exact rules and setup
/// changes, link out to the Spirit Island wiki.
/// </summary>
public static class ScenarioDetails
{
    public static IReadOnlyDictionary<ScenarioId, ScenarioDetail> All { get; } =
        new Dictionary<ScenarioId, ScenarioDetail>
        {
            // Base Game
            [new("blitz")] = new(
                "Invaders strike fast — a Build phase happens before the first Explore. The early game is compressed, rewarding Spirits that can punch immediately."),

            [new("guard-the-isles-heart")] = new(
                "Defend a sacred inner region. Specific interior lands must be kept clear of invaders or you lose; rewards strong board presence and removal."),

            [new("rituals-of-terror")] = new(
                "Invaders are conducting dark rituals, and only the deepest fear can stop them. The win condition shifts towards reaching the highest Terror level."),

            // Branch & Claw
            [new("second-wave")] = new(
                "A two-game scenario: choices and outcomes from the first game carry over into the second wave of invaders that follows."),

            [new("powers-long-forgotten")] = new(
                "Forgotten powers stir on the island — Spirits gain access to additional unique cards and effects beyond their usual decks."),

            [new("ward-the-shores")] = new(
                "Beat back the tide: coastal lands must remain clear of invader buildings or the scenario is lost. Punishes Spirits weak at the shoreline."),

            [new("rituals-of-the-destroying-flame")] = new(
                "Invaders are attempting a destructive fire ritual at the heart of the island. Stop the ritual or burn with it."),

            [new("dahan-insurrection")] = new(
                "The Dahan rise up against the invaders. Their actions, positioning, and survival become central to the win condition — bringing the islanders' fight to the foreground."),

            // Jagged Earth
            [new("elemental-invocation")] = new(
                "Powerful elemental rituals shape the game. Element thresholds and combinations matter more than usual, and timing them right is the path to victory."),

            [new("despicable-theft")] = new(
                "Invaders are stealing something precious from the island. Recover or prevent the theft to win."),

            [new("the-great-river")] = new(
                "A great river runs through the island, forming a special connecting region with its own rules and pressure points."),

            // Feather & Flame
            [new("a-diversity-of-spirits")] = new(
                "A celebration of variety — the wider the spread of styles and complexities across the team, the more this scenario rewards the play."),

            [new("varied-terrains")] = new(
                "An alternative terrain layout where lands carry mixed terrain identity, shifting the calculus of every power that targets terrain."),

            // Nature Incarnate
            [new("destiny-unfolds")] = new(
                "A scenario that weaves a longer-term arc as events unfold on the island, themed for Nature Incarnate."),

            [new("surges-of-colonization")] = new(
                "Colonization arrives in waves. Explore and Build pressure intensifies during specific surge moments, making timing windows tighter than usual."),
        };

    public static ScenarioDetail? For(ScenarioId id) => All.GetValueOrDefault(id);
}
