using Application.Data;
using Application.Features.Games.Dtos;
using Domain.Errors;
using Domain.Models.Friendship;
using Domain.Models.Game;
using Domain.Models.IslandLayout;
using Domain.Models.Static;
using Domain.Models.Static.Data;
using Domain.Models.User;
using Domain.Results;
using Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Games;

/// <summary>
/// Everything Create and Draft derive from the setup half of their command, validated
/// and converted to domain objects — the payload of <see cref="GameFactory.BuildSetupAsync"/>.
/// </summary>
internal sealed record GameSetup(
    IslandChoice Island,
    List<GamePlayer> Players,
    List<PlayedAdversary> Adversaries,
    PlayedScenario? Scenario,
    Difficulty Difficulty,
    DifficultyModifier Modifier,
    GameNote? Note);

/// <summary>
/// Shared factory methods for building domain objects from DTOs.
/// </summary>
internal static class GameFactory
{
    /// <summary>
    /// Runs every server-side (Type 2) check a game setup needs — catalog ids, duplicates,
    /// friendships, island rules, arrangement geometry, difficulty — and builds the domain
    /// objects. Returns the first violation as a domain error. Create and Draft both
    /// write the same setup, so they both come through here.
    /// </summary>
    public static async Task<Result<GameSetup>> BuildSetupAsync(
        IAppDbContext db,
        IGameSetupCommand command,
        UserId ownerId,
        CancellationToken cancellationToken)
    {
        var catalogCheck = ValidateCatalogReferences(
            command.Players, command.Adversaries, command.ScenarioId, command.ExtraBoardId);
        if (catalogCheck.IsFailure)
            return Result.Failure<GameSetup>(catalogCheck.Error);

        var duplicatesCheck = ValidateNoDuplicates(command.Players, command.Adversaries, command.ExtraBoardId);
        if (duplicatesCheck.IsFailure)
            return Result.Failure<GameSetup>(duplicatesCheck.Error);

        var friendshipCheck = await ValidatePlayerFriendships(ownerId, command.Players, db, cancellationToken);
        if (friendshipCheck.IsFailure)
            return Result.Failure<GameSetup>(friendshipCheck.Error);

        var setupCheck = ValidateIslandSetup(
            command.IslandSetupId, command.Players.Count, command.ExtraBoard, command.ThematicMaps);
        if (setupCheck.IsFailure)
            return Result.Failure<GameSetup>(setupCheck.Error);

        var thematicCheck = ValidateThematicBoards(
            command.IslandSetupId, command.Players, command.ExtraBoardId, command.ThematicMaps);
        if (thematicCheck.IsFailure)
            return Result.Failure<GameSetup>(thematicCheck.Error);

        var islandResult = await BuildIslandAsync(
            db, command.IslandSetupId, command.Players.Count + (command.ExtraBoard ? 1 : 0),
            command.ExtraBoardId, command.IslandLayoutJson, command.SavedLayoutId,
            ownerId.Value, cancellationToken);
        if (islandResult.IsFailure)
            return Result.Failure<GameSetup>(islandResult.Error);

        var modifierResult = DifficultyModifier.Create(command.DifficultyModifier);
        if (modifierResult.IsFailure)
            return Result.Failure<GameSetup>(modifierResult.Error);

        var difficultyResult = ComputeDifficulty(
            command.ScenarioId, command.Adversaries, command.ExtraBoard, command.ThematicMaps,
            modifierResult.Value);
        if (difficultyResult.IsFailure)
            return Result.Failure<GameSetup>(difficultyResult.Error);

        GameNote? note = null;
        if (command.Note is not null)
        {
            var noteResult = GameNote.Create(command.Note);
            if (noteResult.IsFailure)
                return Result.Failure<GameSetup>(noteResult.Error);
            note = noteResult.Value;
        }

        return new GameSetup(
            islandResult.Value,
            BuildPlayers(command.Players),
            BuildAdversaries(command.Adversaries),
            BuildScenario(command.ScenarioId),
            difficultyResult.Value,
            modifierResult.Value,
            note);
    }

    private static List<GamePlayer> BuildPlayers(List<GamePlayerDto> dtos) =>
        dtos.Select(p =>
            p.UserId.HasValue
                ? GamePlayer.CreateUserPlayer(
                    new GamePlayerId(Guid.NewGuid()),
                    new BoardId(p.BoardId),
                    new SpiritId(p.SpiritId),
                    p.AspectId is not null ? new AspectId(p.AspectId) : null,
                    new UserId(p.UserId.Value))
                : GamePlayer.CreatePlayer(
                    new GamePlayerId(Guid.NewGuid()),
                    new BoardId(p.BoardId),
                    new SpiritId(p.SpiritId),
                    p.AspectId is not null ? new AspectId(p.AspectId) : null,
                    new Domain.Models.Player.PlayerId(p.PlayerId!.Value)))
            .ToList();

    private static List<PlayedAdversary> BuildAdversaries(List<GameAdversaryDto> dtos) =>
        dtos.Select(a =>
        {
            // Safe to unwrap: ValidateCatalogReferences has already matched every level
            // against a real mode of its adversary, so Create cannot fail here.
            var levelResult = AdversaryLevel.Create(a.Level);
            return new PlayedAdversary(
                new PlayedAdversaryId(Guid.NewGuid()),
                new AdversaryId(a.AdversaryId),
                levelResult.Value);
        }).ToList();

    private static PlayedScenario? BuildScenario(string? scenarioId) =>
        scenarioId is not null
            ? new PlayedScenario(new PlayedScenarioId(Guid.NewGuid()), new ScenarioId(scenarioId))
            : null;

    public static Result<GameResult> BuildResult(GameResultDto dto, Difficulty difficulty, int playerCount)
    {
        var cards = CardsCount.Create(dto.Cards);
        var blight = BlightCount.Create(dto.Blight);
        var dahan = DahanCount.Create(dto.Dahan);
        var scoreMod = ScoreModifier.Create(dto.ScoreModifier);

        if (cards.IsFailure) return Result.Failure<GameResult>(cards.Error);
        if (blight.IsFailure) return Result.Failure<GameResult>(blight.Error);
        if (dahan.IsFailure) return Result.Failure<GameResult>(dahan.Error);
        if (scoreMod.IsFailure) return Result.Failure<GameResult>(scoreMod.Error);

        var scoreResult = ScoreCalculator.Calculate(
            dto.Win, difficulty, dahan.Value, cards.Value, blight.Value, playerCount, scoreMod.Value);

        if (scoreResult.IsFailure) return Result.Failure<GameResult>(scoreResult.Error);

        return GameResult.Create(
            new GameResultId(Guid.NewGuid()),
            dto.Win,
            dto.Duration,
            cards.Value,
            dto.TerrorLevel,
            blight.Value,
            dahan.Value,
            scoreResult.Value,
            scoreMod.Value);
    }

    /// <summary>
    /// Verifies that every spirit / board / aspect / adversary / scenario id in the request
    /// actually exists in the static catalog, and that adversary levels match a real mode
    /// for the chosen adversary. Returns the first violation as a domain error.
    /// </summary>
    private static Result ValidateCatalogReferences(
        List<GamePlayerDto> players,
        List<GameAdversaryDto> adversaries,
        string? scenarioId,
        string? extraBoardId)
    {
        if (!string.IsNullOrEmpty(extraBoardId) && GameData.Boards.All(b => b.Id.Value != extraBoardId))
            return Result.Failure(DomainErrors.Game.UnknownBoard);

        foreach (var p in players)
        {
            if (!string.IsNullOrEmpty(p.SpiritId) && GameData.Spirits.All(s => s.Id.Value != p.SpiritId))
                return Result.Failure(DomainErrors.Game.UnknownSpirit);
            if (!string.IsNullOrEmpty(p.BoardId) && GameData.Boards.All(b => b.Id.Value != p.BoardId))
                return Result.Failure(DomainErrors.Game.UnknownBoard);
            if (!string.IsNullOrEmpty(p.AspectId) && GameData.Aspects.All(a => a.Id.Value != p.AspectId))
                return Result.Failure(DomainErrors.Game.UnknownAspect);
        }

        foreach (var a in adversaries)
        {
            var adv = GameData.Adversaries.FirstOrDefault(x => x.Id.Value == a.AdversaryId);
            if (adv is null)
                return Result.Failure(DomainErrors.Game.UnknownAdversary);
            if (adv.Modes.All(m => m.Level != a.Level))
                return Result.Failure(DomainErrors.Game.UnknownAdversaryLevel);
        }

        if (!string.IsNullOrEmpty(scenarioId) && GameData.Scenarios.All(s => s.Id.Value != scenarioId))
            return Result.Failure(DomainErrors.Game.UnknownScenario);

        return Result.Success();
    }

    /// <summary>
    /// Rejects games where two boards on the island are the same lettered board, or the same
    /// adversary appears twice. Both are game-rule violations that the UI prevents but a direct
    /// API call could send.
    /// </summary>
    private static Result ValidateNoDuplicates(
        List<GamePlayerDto> players,
        List<GameAdversaryDto> adversaries,
        string? extraBoardId)
    {
        // There is one physical copy of each lettered board, so the extra board counts against
        // the same pool the seats draw from.
        var boards = players
            .Select(p => p.BoardId)
            .Append(extraBoardId)
            .Where(id => !string.IsNullOrEmpty(id))
            .ToList();
        if (boards.Count != boards.Distinct().Count())
            return Result.Failure(DomainErrors.Game.DuplicateBoard);

        var advIds = adversaries
            .Where(a => !string.IsNullOrEmpty(a.AdversaryId))
            .Select(a => a.AdversaryId)
            .ToList();
        if (advIds.Count != advIds.Distinct().Count())
            return Result.Failure(DomainErrors.Game.DuplicateAdversary);

        return Result.Success();
    }

    /// <summary>
    /// Validates the island setup matches the player count (+ optional extra board) and the
    /// thematic-maps toggle. Returns Success if the combination is allowed.
    /// </summary>
    /// <remarks>
    /// Whether the extra board has been named is not checked here: that is a required field like
    /// any other and lives in the shared command rules, so it reaches the user against the field
    /// rather than as a general alert. See <c>GameSetupRules.AddGameSetupRules</c>.
    /// </remarks>
    private static Result ValidateIslandSetup(
        string islandSetupId, int playerCount, bool extraBoard, bool thematicMaps)
    {
        if (extraBoard && playerCount > GameRestrictions.MaximumPlayersForExtraBoard)
            return Result.Failure(DomainErrors.Game.ExtraBoardNotAllowed);

        var setup = GameData.IslandSetups.FirstOrDefault(s => s.Id.Value == islandSetupId);
        if (setup is null)
            return Result.Failure(DomainErrors.Game.UnknownIslandSetup);

        var requiredBoards = playerCount + (extraBoard ? 1 : 0);
        if (setup.NumberOfPlayers != requiredBoards)
            return Result.Failure(DomainErrors.Game.IslandSetupPlayerCountMismatch);

        if (thematicMaps)
        {
            // Thematic maps are one fixed island cut into a fixed number of pieces, so there
            // is nothing to arrange and nothing to choose between — a hand-built shape is not
            // a thing that can be played with them.
            if (setup.IsCustom)
                return Result.Failure(DomainErrors.IslandLayout.NotAllowedWithThematicMaps);
            if (IslandSetups.ThematicFor(requiredBoards) is null)
                return Result.Failure(DomainErrors.Game.NoThematicMapForBoardCount);
            if (!setup.IsThematic)
                return Result.Failure(DomainErrors.Game.IslandSetupNotThematic);
            return Result.Success();
        }

        if (setup.IsThematic)
            return Result.Failure(DomainErrors.Game.IslandSetupIsThematic);

        return Result.Success();
    }

    /// <summary>
    /// On the thematic island a board is a region, not a choice: seats take the island's
    /// positions in slot order and the extra board is the position nobody plays. The UI assigns
    /// them exactly that way and locks the pickers, but a direct call could send any letters —
    /// so the pairing is re-checked here like the other UI-prevented rule violations.
    /// </summary>
    private static Result ValidateThematicBoards(
        string islandSetupId, List<GamePlayerDto> players, string? extraBoardId, bool thematicMaps)
    {
        if (!thematicMaps) return Result.Success();
        // ValidateIslandSetup only lets published thematic islands through here, and each of
        // those has a region list — the null branch is a defensive no-op.
        if (ThematicIslandBoards.For(new IslandSetupId(islandSetupId)) is not { } regions)
            return Result.Success();

        for (var i = 0; i < players.Count; i++)
        {
            if (i >= regions.Count || players[i].BoardId != regions[i].Value)
                return Result.Failure(DomainErrors.Game.ThematicBoardMismatch);
        }

        if (!string.IsNullOrEmpty(extraBoardId)
            && (players.Count >= regions.Count || extraBoardId != regions[players.Count].Value))
            return Result.Failure(DomainErrors.Game.ThematicBoardMismatch);

        return Result.Success();
    }

    /// <summary>
    /// Validates the arrangement that goes with the setup: a hand-built island has to bring
    /// its geometry (otherwise the shape would be lost), a published one ignores any that is
    /// supplied, and a referenced library layout has to exist, belong to the caller and cover
    /// the same number of boards. Returns the island to store on the game.
    /// </summary>
    private static async Task<Result<IslandChoice>> BuildIslandAsync(
        IAppDbContext db,
        string islandSetupId,
        int boardCount,
        string? extraBoardId,
        string? layoutJson,
        Guid? savedLayoutId,
        Guid ownerId,
        CancellationToken cancellationToken)
    {
        var setupId = new IslandSetupId(islandSetupId);
        // Whether an extra board is used at all is settled by ValidateIslandSetup, and whether
        // its letter was named by each command's validator (GameSetupRules) — both before this.
        var extra = string.IsNullOrEmpty(extraBoardId) ? null : new BoardId(extraBoardId);

        if (!IslandSetups.IsCustomId(islandSetupId))
            return IslandChoice.Published(setupId, extra);

        var geometryResult = IslandLayoutGeometry.Create(layoutJson);
        if (geometryResult.IsFailure)
            return Result.Failure<IslandChoice>(geometryResult.Error);

        // The setup id says how many boards were on the table; the arrangement has to be of
        // that island, not of some other one the caller happened to be holding.
        if (geometryResult.Value.BoardCount != boardCount)
            return Result.Failure<IslandChoice>(DomainErrors.IslandLayout.BoardCountMismatch);

        if (savedLayoutId is not { } id)
            return new IslandChoice(setupId, geometryResult.Value, ExtraBoard: extra);

        // The key goes through a value converter — compare the id itself, not its .Value.
        var layoutId = new CustomIslandLayoutId(id);
        var saved = await db.CustomIslandLayouts
            .AsNoTracking()
            .FirstOrDefaultAsync(
                l => l.Id == layoutId && l.OwnerId.Value == ownerId,
                cancellationToken);

        if (saved is null)
            return Result.Failure<IslandChoice>(DomainErrors.IslandLayout.NotFound);
        if (saved.BoardCount != boardCount)
            return Result.Failure<IslandChoice>(DomainErrors.IslandLayout.BoardCountMismatch);

        return new IslandChoice(setupId, geometryResult.Value, layoutId, extra);
    }

    /// <summary>
    /// Computes the total difficulty from scenario, adversaries, extra-board and thematic-maps
    /// bonuses, and a manual modifier. Unknown scenario / adversary IDs contribute 0 here — the
    /// stored IDs themselves are validated elsewhere.
    /// </summary>
    private static Result<Difficulty> ComputeDifficulty(
        string? scenarioId,
        List<GameAdversaryDto> adversaries,
        bool extraBoard,
        bool thematicMaps,
        DifficultyModifier modifier)
    {
        var scenarioDifficulty = scenarioId is null
            ? 0
            : GameData.Scenarios.FirstOrDefault(s => s.Id.Value == scenarioId)?.Difficulty ?? 0;

        var adversaryDifficulties = adversaries.Select(a =>
        {
            var adv = GameData.Adversaries.FirstOrDefault(x => x.Id.Value == a.AdversaryId);
            return adv?.Modes.FirstOrDefault(m => m.Level == a.Level)?.Difficulty ?? 0;
        });

        return DifficultyCalculator.Calculate(
            scenarioDifficulty, adversaryDifficulties, extraBoard, thematicMaps, modifier);
    }

    /// <summary>
    /// Validates that all registered users (UserId) in the player list are friends with the game owner.
    /// The owner themselves is excluded from the check.
    /// </summary>
    private static async Task<Result> ValidatePlayerFriendships(
        UserId ownerId,
        List<GamePlayerDto> players,
        IAppDbContext db,
        CancellationToken cancellationToken)
    {
        var otherUserIds = players
            .Where(p => p.UserId.HasValue && p.UserId.Value != ownerId.Value)
            .Select(p => new UserId(p.UserId!.Value))
            .Distinct()
            .ToList();

        if (otherUserIds.Count == 0)
            return Result.Success();

        var friendships = await db.Friendships
            .AsNoTracking()
            .Where(f => f.Status == FriendshipStatus.Accepted &&
                        (f.RequesterId.Value == ownerId.Value || f.AddresseeId.Value == ownerId.Value))
            .ToListAsync(cancellationToken);

        var acceptedFriendIds = friendships
            .Select(f => f.GetOtherUserId(ownerId))
            .ToList();

        var friendIdSet = acceptedFriendIds.ToHashSet();

        var nonFriend = otherUserIds.FirstOrDefault(id => !friendIdSet.Contains(id));
        if (nonFriend is not null)
            return Result.Failure(DomainErrors.Game.PlayerNotFriend);

        return Result.Success();
    }
}
