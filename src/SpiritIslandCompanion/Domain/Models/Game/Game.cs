using Domain.Models.Static;
using Domain.Primitives;

namespace Domain.Models.Game;

public class Game : AggregateRoot<GameId>
{
    public DateTimeOffset StartedAt { get; private set; }
    public GameResult? Result { get; private set; }
    public PlayedScenario? Scenario { get; private set; }
    public IslandSetupId IslandSetupId { get; private set; }
    public Difficulty Difficulty { get; private set; }
    public GameNote? Note { get; private set; }
    public IReadOnlyCollection<PlayedAdversary> PlayedAdversaries => _playedAdversaries.AsReadOnly();
    private List<PlayedAdversary> _playedAdversaries;
    public IReadOnlyCollection<GamePlayer> Players => _players.AsReadOnly();
    private List<GamePlayer> _players;

    private Game(
        GameId id,
        DateTimeOffset startedAt,
        IslandSetupId islandSetupId,
        List<GamePlayer> players,
        List<PlayedAdversary> adversaries,
        PlayedScenario? scenario,
        Difficulty difficulty,
        GameResult? result,
        GameNote? note)
        : base(id)
    {
        StartedAt = startedAt;
        IslandSetupId = islandSetupId;
        Difficulty = difficulty;
        Result = result;
        Note = note;
        _playedAdversaries = adversaries;
        Scenario = scenario;
        _players = players;
    }


    public static Game StartNew(
        GameId id,
        DateTimeOffset startedAt,
        IslandSetupId islandSetupId,
        List<GamePlayer> players,
        List<PlayedAdversary> adversaries,
        PlayedScenario? scenario,
        Difficulty difficultyLevel)
    {
        var game = new Game(id, startedAt, islandSetupId, players, adversaries, scenario, difficultyLevel, null, null);
        return game;
    }

    public static Game Create(
        GameId id,
        DateTimeOffset startedAt,
        IslandSetupId islandSetupId,
        List<GamePlayer> players,
        List<PlayedAdversary> adversaries,
        PlayedScenario? scenario,
        Difficulty difficultyLevel,
        GameResult? result,
        GameNote? note)
    {
        var game = new Game(id, startedAt, islandSetupId, players, adversaries, scenario, difficultyLevel, result, note);
        return game;
    }

    public void Update(
        DateTimeOffset startedAt,
        IslandSetupId islandSetupId,
        List<GamePlayer> players,
        List<PlayedAdversary> adversaries,
        PlayedScenario? scenario,
        Difficulty difficultyLevel,
        GameResult? result,
        GameNote? note)
    {
        StartedAt = startedAt;
        IslandSetupId = islandSetupId;
        Difficulty = difficultyLevel;
        Result = result;
        Note = note;
        _playedAdversaries = adversaries;
        Scenario = scenario;
        _players = players;
    }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    /// <summary>
    /// Empty constructor required for EF Core.
    /// </summary>
    [Obsolete("Empty constructor required for EF Core.")]
    private Game(){}
#pragma warning restore
}