using Domain.Models.Static;
using Domain.Primitives;

namespace Domain.Models.Game;

public class Game : AggregateRoot<GameId>
{
    public DateTimeOffset StartedAt { get; private set; }
    public GameResult? Result { get; private set; }
    public PlayedAdversary? FirstAdversary { get; private set; }
    public PlayedAdversary? SecondAdversary { get; private set; }
    public PlayedScenario? Scenario { get; private set; }
    public IslandSetup IslandSetup { get; private set; }
    public Difficulty Difficulty { get; private set; }
    public GameNote? Note { get; private set; }
    public IReadOnlyCollection<GamePlayer> Players => _players.AsReadOnly();
    private List<GamePlayer> _players;

    private Game(
        GameId id,
        DateTimeOffset startedAt,
        IslandSetup islandSetup,
        List<GamePlayer> players,
        PlayedAdversary? firstAdversary,
        PlayedAdversary? secondAdversary,
        PlayedScenario? scenario,
        Difficulty difficulty,
        GameResult? result,
        GameNote? note)
        : base(id)
    {
        StartedAt = startedAt;
        IslandSetup = islandSetup;
        Difficulty = difficulty;
        Result = result;
        Note = note;
        FirstAdversary = firstAdversary;
        SecondAdversary = secondAdversary;
        Scenario = scenario;
        _players = players;
    }


    public static Game StartNew(
        GameId id,
        DateTimeOffset startedAt,
        IslandSetup islandSetup,
        List<GamePlayer> players,
        PlayedAdversary? firstAdversary,
        PlayedAdversary? secondAdversary,
        PlayedScenario? scenario,
        Difficulty difficultyLevel)
    {
        var game = new Game(id, startedAt, islandSetup, players, firstAdversary, secondAdversary, scenario, difficultyLevel, null, null);
        return game;
    }

    public static Game Create(
        GameId id,
        DateTimeOffset startedAt,
        IslandSetup islandSetup,
        List<GamePlayer> players,
        PlayedAdversary? firstAdversary,
        PlayedAdversary? secondAdversary,
        PlayedScenario? scenario,
        Difficulty difficultyLevel,
        GameResult? result,
        GameNote? note)
    {
        var game = new Game(id, startedAt, islandSetup, players, firstAdversary, secondAdversary, scenario, difficultyLevel, result, note);
        return game;
    }

    public void Update(
        DateTimeOffset startedAt,
        IslandSetup islandSetup,
        List<GamePlayer> players,
        PlayedAdversary? firstAdversary,
        PlayedAdversary? secondAdversary,
        PlayedScenario? scenario,
        Difficulty difficultyLevel,
        GameResult? result,
        GameNote? note)
    {
        StartedAt = startedAt;
        IslandSetup = islandSetup;
        Difficulty = difficultyLevel;
        Result = result;
        Note = note;
        FirstAdversary = firstAdversary;
        SecondAdversary = secondAdversary;
        Scenario = scenario;
        _players = players;
    }


}