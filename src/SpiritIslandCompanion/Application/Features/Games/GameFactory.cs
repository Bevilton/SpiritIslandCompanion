using Application.Data;
using Application.Features.Games.Dtos;
using Domain.Errors;
using Domain.Models.Friendship;
using Domain.Models.Game;
using Domain.Models.Static;
using Domain.Models.Static.Data;
using Domain.Models.User;
using Domain.Results;
using Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Games;

/// <summary>
/// Shared factory methods for building domain objects from DTOs.
/// </summary>
internal static class GameFactory
{
    public static List<GamePlayer> BuildPlayers(List<GamePlayerDto> dtos) =>
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

    public static List<PlayedAdversary> BuildAdversaries(List<GameAdversaryDto> dtos) =>
        dtos.Select(a =>
        {
            var levelResult = AdversaryLevel.Create(a.Level);
            return new PlayedAdversary(
                new PlayedAdversaryId(Guid.NewGuid()),
                new AdversaryId(a.AdversaryId),
                levelResult.Value);
        }).ToList();

    public static PlayedScenario? BuildScenario(string? scenarioId) =>
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
    /// Validates the island setup matches the player count (+ optional extra board) and the
    /// thematic-maps toggle. Returns Success if the combination is allowed.
    /// </summary>
    public static Result ValidateIslandSetup(string islandSetupId, int playerCount, bool extraBoard, bool thematicMaps)
    {
        if (extraBoard && playerCount > GameRestrictions.MaximumPlayersForExtraBoard)
            return Result.Failure(DomainErrors.Game.ExtraBoardNotAllowed);

        var setup = GameData.IslandSetups.FirstOrDefault(s => s.Id.Value == islandSetupId);
        if (setup is null)
            return Result.Failure(DomainErrors.Game.UnknownIslandSetup);

        var requiredBoards = playerCount + (extraBoard ? 1 : 0);
        if (setup.NumberOfPlayers != requiredBoards)
            return Result.Failure(DomainErrors.Game.IslandSetupPlayerCountMismatch);

        if (thematicMaps && !setup.IsThematic)
            return Result.Failure(DomainErrors.Game.IslandSetupNotThematic);

        if (!thematicMaps && setup.IsThematic)
            return Result.Failure(DomainErrors.Game.IslandSetupIsThematic);

        return Result.Success();
    }

    /// <summary>
    /// Computes the total difficulty from scenario, adversaries, extra-board and thematic-maps
    /// bonuses, and a manual modifier. Unknown scenario / adversary IDs contribute 0 here — the
    /// stored IDs themselves are validated elsewhere.
    /// </summary>
    public static Result<Difficulty> ComputeDifficulty(
        string? scenarioId,
        List<GameAdversaryDto> adversaries,
        bool extraBoard,
        bool thematicMaps,
        int modifier)
    {
        var modifierResult = DifficultyModifier.Create(modifier);
        if (modifierResult.IsFailure) return Result.Failure<Difficulty>(modifierResult.Error);

        var scenarioDifficulty = scenarioId is null
            ? 0
            : GameData.Scenarios.FirstOrDefault(s => s.Id.Value == scenarioId)?.Difficulty ?? 0;

        var adversaryDifficulties = adversaries.Select(a =>
        {
            var adv = GameData.Adversaries.FirstOrDefault(x => x.Id.Value == a.AdversaryId);
            return adv?.Modes.FirstOrDefault(m => m.Level == a.Level)?.Difficulty ?? 0;
        });

        return DifficultyCalculator.Calculate(
            scenarioDifficulty, adversaryDifficulties, extraBoard, thematicMaps, modifierResult.Value);
    }

    /// <summary>
    /// Validates that all registered users (UserId) in the player list are friends with the game owner.
    /// The owner themselves is excluded from the check.
    /// </summary>
    public static async Task<Result> ValidatePlayerFriendships(
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
