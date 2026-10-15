using Application.Abstractions;
using Application.Data;
using Application.Features.Games.Dtos;
using Domain.Models.Game;
using Domain.Models.User;
using Domain.Results;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Games;

/// <summary>
/// Lists all games where the user is the owner or a participant (friend-player).
/// </summary>
public sealed record ListGamesQuery(Guid UserId) : IQuery<List<ListGamesResponse>>;

/// <param name="UserId">
/// The account in the seat, when a friend (or the reader) sat in it. With
/// <paramref name="PlayerId"/> this is what lets a list be narrowed to one person —
/// the name alone can't, since two locals may share one.
/// </param>
public sealed record GamePlayerSummary(
    string Name,
    PlayerKind Kind,
    string SpiritId,
    string BoardId,
    Guid? UserId,
    Guid? PlayerId);

/// <param name="CustomLayoutName">
/// The saved layout the island came from, when the player set one up out of their library.
/// Null for published layouts, one-off hand-built islands, and layouts deleted since.
/// </param>
/// <param name="CustomLayoutId">
/// That layout's id, for grouping games by the shape they were played on. Set even when the
/// layout has been deleted since — <paramref name="CustomLayoutName"/> is what says whether
/// it still exists.
/// </param>
/// <param name="OwnerId">
/// Who recorded it. Everyone at the table can read the game, but only the owner can finish
/// or delete it — so a list has to know whose it is before it offers either.
/// </param>
public sealed record ListGamesResponse(
    Guid Id,
    Guid OwnerId,
    DateTimeOffset StartedAt,
    int Difficulty,
    bool IsCompleted,
    bool? Win,
    int? Score,
    int PlayerCount,
    List<GameAdversaryResponse> Adversaries,
    string? ScenarioId,
    string IslandSetupId,
    Guid? CustomLayoutId,
    string? CustomLayoutName,
    List<GamePlayerSummary> Players);

internal sealed class ListGamesHandler(IAppDbContext db, SeatNameResolver seatNames) : IQueryHandler<ListGamesQuery, List<ListGamesResponse>>
{
    public async Task<Result<List<ListGamesResponse>>> Handle(ListGamesQuery request, CancellationToken cancellationToken)
    {
        var games = await db.Games
            .InvolvingUser(new UserId(request.UserId))
            .OrderByDescending(g => g.StartedAt)
            .ToListAsync(cancellationToken);

        var names = await seatNames.LoadAsync(games, cancellationToken);

        // Saved-layout names, for the games set up from the player's layout library. The game
        // keeps its own geometry, so a layout deleted since simply resolves to no name here.
        var layoutIds = games
            .Where(g => g.CustomLayoutId is not null)
            .Select(g => g.CustomLayoutId!)
            .Distinct()
            .ToList();
        var layoutLookup = (await db.CustomIslandLayouts
            .Where(l => layoutIds.Contains(l.Id))
            .Select(l => new { l.Id, Name = l.Name.Value })
            .ToListAsync(cancellationToken))
            .ToDictionary(l => l.Id, l => l.Name);

        var response = games.Select(g => new ListGamesResponse(
            g.Id.Value,
            g.OwnerId.Value,
            g.StartedAt,
            g.Difficulty.Value,
            g.Result is not null,
            g.Result?.Outcome.Win,
            g.Result?.Score.Value,
            g.Players.Count,
            g.PlayedAdversaries.Select(a => a.ToResponse()).ToList(),
            g.ScenarioId?.Value,
            g.IslandSetupId.Value,
            g.CustomLayoutId?.Value,
            g.CustomLayoutId is { } layoutId ? layoutLookup.GetValueOrDefault(layoutId) : null,
            g.Players.Select(p => Summarise(p, request.UserId, names)).ToList())).ToList();

        return response;
    }

    private static GamePlayerSummary Summarise(GamePlayer seat, Guid currentUserId, SeatNames names)
    {
        var (name, kind) = names.Describe(seat.UserId?.Value, seat.PlayerId?.Value, currentUserId);
        return new GamePlayerSummary(
            name, kind, seat.SpiritId.Value, seat.StartingBoard.Value, seat.UserId?.Value, seat.PlayerId?.Value);
    }
}
