using System.Diagnostics.CodeAnalysis;
using Domain.Models.IslandLayout;
using Domain.Models.Static;
using Domain.Models.User;
using Domain.Primitives;

namespace Domain.Models.Game;

public class Game : AggregateRoot<GameId>
{
    public DateTimeOffset StartedAt { get; private set; }
    public GameResult? Result { get; private set; }
    public PlayedScenario? Scenario { get; private set; }
    public IslandSetupId IslandSetupId { get; private set; }

    /// <summary>
    /// Where the boards actually sat, for a hand-built island; null when the game used one
    /// of the published layouts, whose geometry is already known from its id.
    /// <para>
    /// This is the game's own copy, not a pointer into the player's layout library: a game
    /// records what was on the table that evening, and editing or deleting a saved layout
    /// afterwards must not rewrite it.
    /// </para>
    /// </summary>
    public IslandLayoutGeometry? IslandLayout { get; private set; }

    /// <summary>
    /// The saved layout this island came from, when the player picked one out of their
    /// library rather than arranging a one-off. Kept so plays can be counted per layout;
    /// the layout may since have been renamed, reshaped or deleted.
    /// </summary>
    public CustomIslandLayoutId? CustomLayoutId { get; private set; }

    /// <summary>
    /// The board on the island that nobody played — the extra board some games add to raise
    /// the difficulty. Null when the game used one board per player.
    /// <para>
    /// Which lettered board it is matters: a board is a specific set of terrains, so an island
    /// of A/B/C reads differently from A/B/E. Null on games recorded before this was asked for,
    /// where the extra board's letter is simply not known.
    /// </para>
    /// </summary>
    public BoardId? ExtraBoard { get; private set; }

    public Difficulty Difficulty { get; private set; }
    public DifficultyModifier DifficultyModifier { get; private set; }
    public GameNote? Note { get; private set; }
    public UserId OwnerId { get; private init; }
    public IReadOnlyCollection<PlayedAdversary> PlayedAdversaries => _playedAdversaries.AsReadOnly();
    private List<PlayedAdversary> _playedAdversaries;
    public IReadOnlyCollection<GamePlayer> Players => _players.AsReadOnly();
    private List<GamePlayer> _players;

    private Game(
        GameId id,
        DateTimeOffset startedAt,
        IslandChoice island,
        List<GamePlayer> players,
        List<PlayedAdversary> adversaries,
        PlayedScenario? scenario,
        Difficulty difficulty,
        DifficultyModifier difficultyModifier,
        GameResult? result,
        GameNote? note,
        UserId ownerId)
        : base(id)
    {
        StartedAt = startedAt;
        SetIsland(island);
        Difficulty = difficulty;
        DifficultyModifier = difficultyModifier;
        Result = result;
        Note = note;
        _playedAdversaries = adversaries;
        Scenario = scenario;
        _players = players;
        OwnerId = ownerId;
    }


    public static Game StartNew(
        GameId id,
        DateTimeOffset startedAt,
        IslandChoice island,
        List<GamePlayer> players,
        List<PlayedAdversary> adversaries,
        PlayedScenario? scenario,
        Difficulty difficultyLevel,
        DifficultyModifier difficultyModifier,
        GameNote? note,
        UserId ownerId)
    {
        var game = new Game(id, startedAt, island, players, adversaries, scenario, difficultyLevel, difficultyModifier, null, note, ownerId);
        return game;
    }

    public static Game Create(
        GameId id,
        DateTimeOffset startedAt,
        IslandChoice island,
        List<GamePlayer> players,
        List<PlayedAdversary> adversaries,
        PlayedScenario? scenario,
        Difficulty difficultyLevel,
        DifficultyModifier difficultyModifier,
        GameResult? result,
        GameNote? note,
        UserId ownerId)
    {
        var game = new Game(id, startedAt, island, players, adversaries, scenario, difficultyLevel, difficultyModifier, result, note, ownerId);
        return game;
    }

    /// <summary>
    /// The same evening written down again in somebody else's name — what a friendship ending
    /// leaves both sides with, so neither loses the games they played together.
    /// <para>
    /// The seats are the caller's to supply: who was at the table reads differently from the
    /// other side of it, and only the caller knows which accounts the new owner may still name.
    /// Everything else is copied verbatim, down to the arrangement of the boards.
    /// </para>
    /// <para>
    /// The one deliberate omission is <see cref="CustomLayoutId"/>: it points into the original
    /// owner's layout library, which the new owner has no entry in. The geometry itself travels
    /// with the copy, so the island is not lost — only the link back to a shape that was never
    /// theirs.
    /// </para>
    /// </summary>
    public Game CopyForOwner(GameId id, UserId ownerId, List<GamePlayer> players)
    {
        // Fresh instances of every owned value: two aggregates must not share one.
        var island = new IslandChoice(
            IslandSetupId with { },
            IslandLayout is null ? null : IslandLayout with { },
            SavedLayoutId: null,
            ExtraBoard is null ? null : ExtraBoard with { });

        var adversaries = _playedAdversaries
            .Select(a => new PlayedAdversary(
                new PlayedAdversaryId(Guid.NewGuid()), a.AdversaryId with { }, a.Level with { }))
            .ToList();

        var scenario = Scenario is null
            ? null
            : new PlayedScenario(new PlayedScenarioId(Guid.NewGuid()), Scenario.ScenarioId with { });

        var result = Result is null
            ? null
            : GameResult.Create(
                new GameResultId(Guid.NewGuid()),
                Result.Win,
                Result.Duration,
                Result.Cards with { },
                Result.TerrorLevel,
                Result.Blight with { },
                Result.Dahan with { },
                Result.Score with { },
                Result.ScoreModifier with { });

        return new Game(
            id,
            StartedAt,
            island,
            players,
            adversaries,
            scenario,
            Difficulty with { },
            DifficultyModifier with { },
            result,
            Note is null ? null : Note with { },
            // A copy for the same reason the values above are copied: several games can be
            // handed to one owner in a single operation, and they must not share the instance.
            ownerId with { });
    }

    /// <summary>Records the outcome of a drafted game, leaving the setup untouched.</summary>
    public void Complete(GameResult result, GameNote? note)
    {
        Result = result;
        Note = note;
    }

    /// Setting the island through a method is what the constructor needs, but it also hides
    /// the assignment from the compiler's definite-assignment check — hence the annotation.
    [MemberNotNull(nameof(IslandSetupId))]
    private void SetIsland(IslandChoice island)
    {
        IslandSetupId = island.SetupId;
        IslandLayout = island.Layout;
        CustomLayoutId = island.SavedLayoutId;
        ExtraBoard = island.ExtraBoard;
    }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    /// <summary>
    /// Empty constructor required for EF Core.
    /// </summary>
    [Obsolete("Empty constructor required for EF Core.")]
    private Game(){}
#pragma warning restore
}
