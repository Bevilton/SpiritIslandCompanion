using Application.Data;
using Application.Features.Friendships;
using Application.Features.Games.Dtos;
using Domain.Errors;
using Domain.Models.Game;
using Domain.Models.IslandLayout;
using Domain.Models.Player;
using Domain.Models.Static;
using Domain.Models.Static.Data;
using Domain.Models.User;
using Domain.Results;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Games;

/// <summary>
/// Turns the setup half of a game-writing command into the table the domain accepts, a
/// <see cref="GameSetup"/>. On the way it checks what only this layer can: that every account
/// seated is a friend of the owner, and that a referenced library layout exists and is the
/// caller's. Whether the ids name catalogue entries, and whether the table is one the game
/// allows — no board twice, an island cut for the boards in play, the thematic pairing, a
/// hand-built island's arrangement — is the setup's own to decide, in <see cref="GameSetup.Create"/>.
/// <para>
/// The field-bound rules — required fields, ranges — are not repeated here either. They run
/// first, in <see cref="CreateGameValidator"/>, and reach the user against the field
/// concerned; by the time a command arrives here every required id is present.
/// </para>
/// </summary>
internal sealed class GameSetupFactory(IAppDbContext db)
{
    public async Task<Result<GameSetup>> BuildAsync(
        CreateGameCommand command, UserId ownerId, CancellationToken cancellationToken)
    {
        // Looked up here as well as by the setup: whether the island is a hand-built one
        // decides what this layer has to carry (the arrangement).
        var setup = GameData.IslandSetups.FirstOrDefault(s => s.Id.Value == command.IslandSetupId);
        if (setup is null)
            return Result.Failure<GameSetup>(DomainErrors.Game.UnknownIslandSetup);

        var modifier = DifficultyModifier.Create(command.DifficultyModifier);
        if (modifier.IsFailure)
            return Result.Failure<GameSetup>(modifier.Error);

        var adversaries = BuildAdversaries(command.Adversaries);
        if (adversaries.IsFailure)
            return Result.Failure<GameSetup>(adversaries.Error);

        var friendships = await ValidatePlayerFriendshipsAsync(ownerId, command.Players, cancellationToken);
        if (friendships.IsFailure)
            return Result.Failure<GameSetup>(friendships.Error);

        var island = await BuildIslandAsync(setup, command, ownerId, cancellationToken);
        if (island.IsFailure)
            return Result.Failure<GameSetup>(island.Error);

        return GameSetup.Create(
            island.Value,
            BuildPlayers(command.Players),
            adversaries.Value,
            Optional(command.ScenarioId) is { } scenarioId ? new ScenarioId(scenarioId) : null,
            modifier.Value);
    }

    /// <summary>
    /// Every registered account seated in the game has to be a friend of the owner, the owner
    /// themselves excepted. Seating someone puts the game in their history too, and that is
    /// only theirs to receive from a friend.
    /// </summary>
    private async Task<Result> ValidatePlayerFriendshipsAsync(
        UserId ownerId, List<GamePlayerDto> players, CancellationToken cancellationToken)
    {
        var guests = players
            .Where(p => p.UserId is { } id && id != ownerId.Value)
            .Select(p => new UserId(p.UserId!.Value))
            .Distinct()
            .ToList();

        if (guests.Count == 0)
            return Result.Success();

        var friends = await db.Friendships
            .AsNoTracking()
            .Accepted()
            .Involving(ownerId)
            .Select(f => f.RequesterId == ownerId ? f.AddresseeId : f.RequesterId)
            .ToListAsync(cancellationToken);

        return guests.Except(friends).Any()
            ? Result.Failure(DomainErrors.Game.PlayerNotFriend)
            : Result.Success();
    }

    /// <summary>
    /// The island to store on the game. A published layout has nothing to remember beyond its
    /// id, and any arrangement sent along with one is ignored. A hand-built island brings its
    /// arrangement, parsed here where the JSON is — whether one is there at all, and of the
    /// right number of boards, is the game's own rule. A referenced library layout has to
    /// exist, belong to the caller and cover the same number of boards.
    /// </summary>
    private async Task<Result<IslandChoice>> BuildIslandAsync(
        IslandSetup setup, CreateGameCommand command, UserId ownerId, CancellationToken cancellationToken)
    {
        var setupId = new IslandSetupId(command.IslandSetupId);
        var extraBoard = Optional(command.ExtraBoardId) is { } extraBoardId ? new BoardId(extraBoardId) : null;

        if (!setup.IsCustom)
            return IslandChoice.Published(setupId, extraBoard);

        IslandLayoutGeometry? layout = null;
        if (command.IslandLayoutJson is not null)
        {
            var geometry = IslandLayoutGeometry.Create(command.IslandLayoutJson);
            if (geometry.IsFailure)
                return Result.Failure<IslandChoice>(geometry.Error);
            layout = geometry.Value;
        }

        if (command.SavedLayoutId is not { } savedLayoutId)
            return new IslandChoice(setupId, layout, ExtraBoard: extraBoard);

        var layoutId = new CustomIslandLayoutId(savedLayoutId);
        var saved = await db.CustomIslandLayouts
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == layoutId && l.OwnerId == ownerId, cancellationToken);

        if (saved is null)
            return Result.Failure<IslandChoice>(DomainErrors.IslandLayout.NotFound);
        if (saved.BoardCount != setup.BoardCount)
            return Result.Failure<IslandChoice>(DomainErrors.IslandLayout.BoardCountMismatch);

        return new IslandChoice(setupId, layout, layoutId, extraBoard);
    }

    private static List<GamePlayer> BuildPlayers(List<GamePlayerDto> seats) =>
        seats.Select(seat =>
        {
            var board = new BoardId(seat.BoardId);
            var spirit = new SpiritId(seat.SpiritId);
            var aspect = Optional(seat.AspectId) is { } aspectId ? new AspectId(aspectId) : null;

            return seat.UserId is { } userId
                ? GamePlayer.ForUser(board, spirit, aspect, new UserId(userId))
                : GamePlayer.ForLocalPlayer(board, spirit, aspect, new PlayerId(seat.PlayerId!.Value));
        }).ToList();

    /// <summary>
    /// The adversaries as played. Only the level's range is settled here; whether it is a level
    /// the adversary is actually published at is the setup's to check.
    /// </summary>
    private static Result<List<PlayedAdversary>> BuildAdversaries(List<GameAdversaryDto> played)
    {
        var adversaries = new List<PlayedAdversary>(played.Count);
        foreach (var adversary in played)
        {
            var level = AdversaryLevel.Create(adversary.Level);
            if (level.IsFailure)
                return Result.Failure<List<PlayedAdversary>>(level.Error);

            adversaries.Add(PlayedAdversary.Create(new AdversaryId(adversary.AdversaryId), level.Value));
        }

        return adversaries;
    }

    /// <summary>An optional id as a form sends it: null and empty both mean "none".</summary>
    private static string? Optional(string? id) => string.IsNullOrEmpty(id) ? null : id;
}
