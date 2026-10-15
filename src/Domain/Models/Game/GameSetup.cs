using Domain.Errors;
using Domain.Models.Static;
using Domain.Models.Static.Data;
using Domain.Results;
using Domain.Services;

namespace Domain.Models.Game;

/// <summary>
/// The table as it was laid: which island, who sat where playing what, which adversaries and
/// scenario were in play, and the house modifier — checked against the catalogue and the rules
/// of the game, with the difficulty all of that adds up to.
/// <para>
/// This is the one way to describe a table to <see cref="Game"/>. A setup exists only once
/// <see cref="Create"/> has accepted it, so a game can be written down without checking its
/// table again, and its difficulty is never a number a caller supplied but always the one the
/// setup computed. It is an argument, not a part of the aggregate: the game stores the pieces.
/// </para>
/// </summary>
public sealed class GameSetup
{
    public IslandChoice Island { get; }
    public IReadOnlyList<GamePlayer> Players { get; }
    public IReadOnlyList<PlayedAdversary> Adversaries { get; }

    /// <summary>The scenario the game was played under, or null for an open game.</summary>
    public ScenarioId? ScenarioId { get; }

    public DifficultyModifier DifficultyModifier { get; }

    /// <summary>What the table adds up to — see <see cref="DifficultyCalculator"/>.</summary>
    public Difficulty Difficulty { get; }

    private GameSetup(
        IslandChoice island,
        IReadOnlyList<GamePlayer> players,
        IReadOnlyList<PlayedAdversary> adversaries,
        ScenarioId? scenarioId,
        DifficultyModifier difficultyModifier,
        Difficulty difficulty)
    {
        Island = island;
        Players = players;
        Adversaries = adversaries;
        ScenarioId = scenarioId;
        DifficultyModifier = difficultyModifier;
        Difficulty = difficulty;
    }

    /// <summary>
    /// The rules a table has to satisfy before it is a game at all, whoever asks to write it
    /// down. Every id has to name a catalogue entry: a spirit, a board, an aspect of that
    /// spirit, an adversary at one of its published levels, a scenario, an island layout. Then
    /// the table itself: at least one seat; no lettered board twice, the extra board included,
    /// since there is one physical copy of each; no adversary twice; an extra board only at a
    /// table small enough to add one; an island cut for exactly the boards in play; on the
    /// thematic island, every seat on the region belonging to its position; and for a
    /// hand-built island, the arrangement itself, of that many boards.
    /// <para>
    /// The application layer checks what it can earlier, where a failure can be pinned to a
    /// field, and what only it can — that a seated account is a friend of the owner, that a
    /// referenced library layout is the caller's. This is the guarantee behind it: no caller
    /// can construct a game that breaks these, whatever it skipped.
    /// </para>
    /// </summary>
    public static Result<GameSetup> Create(
        IslandChoice island,
        IReadOnlyList<GamePlayer> players,
        IReadOnlyList<PlayedAdversary> adversaries,
        ScenarioId? scenarioId,
        DifficultyModifier difficultyModifier)
    {
        if (players.Count == 0)
            return Result.Failure<GameSetup>(DomainErrors.Game.PlayersRequired);

        var references = ResolveReferences(island, players, adversaries, scenarioId);
        if (references.IsFailure)
            return Result.Failure<GameSetup>(references.Error);

        var table = ValidateTable(island, players, adversaries, references.Value.IslandSetup);
        if (table.IsFailure)
            return Result.Failure<GameSetup>(table.Error);

        // Keyed on what the table is, not on what the caller said it was: the extra board is
        // the one named, and the thematic bonus follows the island actually picked.
        var difficulty = DifficultyCalculator.Calculate(
            references.Value.Scenario?.Difficulty ?? 0,
            references.Value.AdversaryModes.Select(m => m.Difficulty),
            usesExtraBoard: island.ExtraBoard is not null,
            usesThematicMaps: references.Value.IslandSetup.IsThematic,
            difficultyModifier);

        return new GameSetup(island, players, adversaries, scenarioId, difficultyModifier, difficulty);
    }

    /// <summary>
    /// The catalogue entries the setup refers to, once every id has been checked against it.
    /// <see cref="AdversaryModes"/> is in the table's own order, one mode per adversary.
    /// </summary>
    private sealed record CatalogueReferences(
        IslandSetup IslandSetup, Scenario? Scenario, IReadOnlyList<AdversaryMode> AdversaryModes);

    private static Result<CatalogueReferences> ResolveReferences(
        IslandChoice island,
        IReadOnlyList<GamePlayer> players,
        IReadOnlyList<PlayedAdversary> adversaries,
        ScenarioId? scenarioId)
    {
        if (island.ExtraBoard is { } extraBoard && !IsKnownBoard(extraBoard))
            return Result.Failure<CatalogueReferences>(DomainErrors.Game.UnknownBoard);

        foreach (var seat in players)
        {
            if (!GameData.Spirits.Any(s => s.Id == seat.SpiritId))
                return Result.Failure<CatalogueReferences>(DomainErrors.Game.UnknownSpirit);
            if (!IsKnownBoard(seat.StartingBoard))
                return Result.Failure<CatalogueReferences>(DomainErrors.Game.UnknownBoard);

            if (seat.AspectId is { } aspectId)
            {
                var aspect = GameData.Aspects.FirstOrDefault(a => a.Id == aspectId);
                if (aspect is null)
                    return Result.Failure<CatalogueReferences>(DomainErrors.Game.UnknownAspect);
                if (aspect.SpiritId != seat.SpiritId)
                    return Result.Failure<CatalogueReferences>(DomainErrors.Game.AspectMismatch);
            }
        }

        var modes = new List<AdversaryMode>(adversaries.Count);
        foreach (var played in adversaries)
        {
            var adversary = GameData.Adversaries.FirstOrDefault(a => a.Id == played.AdversaryId);
            if (adversary is null)
                return Result.Failure<CatalogueReferences>(DomainErrors.Game.UnknownAdversary);

            var mode = adversary.Modes.FirstOrDefault(m => m.Level == played.Level.Value);
            if (mode is null)
                return Result.Failure<CatalogueReferences>(DomainErrors.Game.UnknownAdversaryLevel);

            modes.Add(mode);
        }

        Scenario? scenario = null;
        if (scenarioId is not null)
        {
            scenario = GameData.Scenarios.FirstOrDefault(s => s.Id == scenarioId);
            if (scenario is null)
                return Result.Failure<CatalogueReferences>(DomainErrors.Game.UnknownScenario);
        }

        var setup = GameData.IslandSetups.FirstOrDefault(s => s.Id == island.SetupId);
        if (setup is null)
            return Result.Failure<CatalogueReferences>(DomainErrors.Game.UnknownIslandSetup);

        return new CatalogueReferences(setup, scenario, modes);
    }

    private static Result ValidateTable(
        IslandChoice island,
        IReadOnlyList<GamePlayer> players,
        IReadOnlyList<PlayedAdversary> adversaries,
        IslandSetup setup)
    {
        var boards = players.Select(p => p.StartingBoard).ToList();
        if (island.ExtraBoard is not null)
            boards.Add(island.ExtraBoard);
        if (boards.Distinct().Count() != boards.Count)
            return Result.Failure(DomainErrors.Game.DuplicateBoard);

        if (adversaries.Select(a => a.AdversaryId).Distinct().Count() != adversaries.Count)
            return Result.Failure(DomainErrors.Game.DuplicateAdversary);

        var extraBoards = island.ExtraBoard is null ? 0 : 1;
        if (extraBoards == 1 && players.Count > GameRestrictions.MaximumPlayersForExtraBoard)
            return Result.Failure(DomainErrors.Game.ExtraBoardNotAllowed);
        if (setup.BoardCount != players.Count + extraBoards)
            return Result.Failure(DomainErrors.Game.IslandSetupPlayerCountMismatch);

        // On the thematic island a board is a region, not a choice: seats take the island's
        // positions in slot order, and the extra board is the position nobody plays.
        if (setup.IsThematic && ThematicIslandBoards.For(setup.Id) is { } regions)
        {
            for (var slot = 0; slot < players.Count; slot++)
            {
                if (slot >= regions.Count || players[slot].StartingBoard != regions[slot])
                    return Result.Failure(DomainErrors.Game.ThematicBoardMismatch);
            }

            if (island.ExtraBoard is { } extra
                && (players.Count >= regions.Count || extra != regions[players.Count]))
                return Result.Failure(DomainErrors.Game.ThematicBoardMismatch);
        }

        // A hand-built island is only known by its arrangement, so the arrangement has to be
        // there — and be of this island, not of some other one the caller was holding.
        if (setup.IsCustom)
        {
            if (island.Layout is null)
                return Result.Failure(DomainErrors.IslandLayout.GeometryRequired);
            if (island.Layout.BoardCount != setup.BoardCount)
                return Result.Failure(DomainErrors.IslandLayout.BoardCountMismatch);
        }

        return Result.Success();
    }

    private static bool IsKnownBoard(BoardId id) => GameData.Boards.Any(b => b.Id == id);
}
