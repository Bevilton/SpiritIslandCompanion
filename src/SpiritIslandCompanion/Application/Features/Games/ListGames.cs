using Application.Abstractions;
using Application.Data;
using Domain.Models.Game;
using Domain.Results;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Games;

/// <summary>
/// Lists all games where the user is the owner or a participant (friend-player).
/// </summary>
public sealed record ListGamesQuery(Guid UserId) : IQuery<List<ListGamesResponse>>;

public enum GamePlayerKind { Me, Friend, Local, Unassigned }

public sealed record GamePlayerSummary(
    string Name,
    GamePlayerKind Kind,
    string SpiritId);

/// <param name="CustomLayoutName">
/// The saved layout the island came from, when the player set one up out of their library.
/// Null for published layouts, one-off hand-built islands, and layouts deleted since.
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
    List<Dtos.GameAdversaryResponse> Adversaries,
    string? ScenarioId,
    string IslandSetupId,
    string? CustomLayoutName,
    List<GamePlayerSummary> Players);

internal sealed class ListGamesHandler(IAppDbContext db) : IQueryHandler<ListGamesQuery, List<ListGamesResponse>>
{
    public async Task<Result<List<ListGamesResponse>>> Handle(ListGamesQuery request, CancellationToken cancellationToken)
    {
        var games = await db.Games
            .InvolvingUser(request.UserId)
            .OrderByDescending(g => g.StartedAt)
            .ToListAsync(cancellationToken);

        // Resolve names for every user / local player referenced. EF's configured
        // HasConversion lets us pass the value-object types directly into Contains.
        var userIds = games.SelectMany(g => g.Players)
            .Where(p => p.UserId is not null)
            .Select(p => p.UserId!)
            .Distinct()
            .ToList();
        var userLookup = (await db.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToListAsync(cancellationToken))
            .ToDictionary(u => u.Id.Value, u => u.DisplayName);

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

        var playerIds = games.SelectMany(g => g.Players)
            .Where(p => p.PlayerId is not null)
            .Select(p => p.PlayerId!)
            .Distinct()
            .ToList();
        var playerLookup = (await db.Players
            .AsNoTracking()
            .Where(p => playerIds.Contains(p.Id))
            .ToListAsync(cancellationToken))
            .ToDictionary(p => p.Id.Value, p => p.Name.Value);

        var response = games.Select(g => new ListGamesResponse(
            g.Id.Value,
            g.OwnerId.Value,
            g.StartedAt,
            g.Difficulty.Value,
            g.Result is not null,
            g.Result?.Win,
            g.Result?.Score.Value,
            g.Players.Count,
            g.PlayedAdversaries.Select(a => new Dtos.GameAdversaryResponse(a.AdversaryId.Value, a.Level.Value)).ToList(),
            g.Scenario?.ScenarioId.Value,
            g.IslandSetupId.Value,
            g.CustomLayoutId is { } layoutId ? layoutLookup.GetValueOrDefault(layoutId) : null,
            g.Players.Select(p => ResolvePlayer(p, request.UserId, userLookup, playerLookup)).ToList())).ToList();

        return response;
    }

    private static GamePlayerSummary ResolvePlayer(
        GamePlayer p,
        Guid currentUserId,
        Dictionary<Guid, string> users,
        Dictionary<Guid, string> players)
    {
        if (p.UserId is { } uid)
        {
            if (uid.Value == currentUserId)
            {
                return new GamePlayerSummary("You", GamePlayerKind.Me, p.SpiritId.Value);
            }
            return new GamePlayerSummary(
                users.GetValueOrDefault(uid.Value, "Unknown"),
                GamePlayerKind.Friend,
                p.SpiritId.Value);
        }
        if (p.PlayerId is { } pid)
        {
            return new GamePlayerSummary(
                players.GetValueOrDefault(pid.Value, "Unknown"),
                GamePlayerKind.Local,
                p.SpiritId.Value);
        }
        return new GamePlayerSummary("Unassigned", GamePlayerKind.Unassigned, p.SpiritId.Value);
    }
}
