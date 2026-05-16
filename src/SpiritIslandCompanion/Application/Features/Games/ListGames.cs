using Application.Abstractions;
using Application.Data;
using Domain.Models.Game;
using Domain.Models.User;
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

public sealed record ListGamesResponse(
    Guid Id,
    DateTimeOffset StartedAt,
    int Difficulty,
    bool IsCompleted,
    bool? Win,
    int? Score,
    int PlayerCount,
    List<string> AdversaryIds,
    string? ScenarioId,
    List<GamePlayerSummary> Players);

internal sealed class ListGamesHandler(IAppDbContext db) : IQueryHandler<ListGamesQuery, List<ListGamesResponse>>
{
    public async Task<Result<List<ListGamesResponse>>> Handle(ListGamesQuery request, CancellationToken cancellationToken)
    {
        var games = await db.Games
            .AsNoTracking()
            .Include(g => g.Players)
            .Include(g => g.PlayedAdversaries)
            .Include(g => g.Result)
            .Include(g => g.Scenario)
            .Where(g => g.OwnerId.Value == request.UserId ||
                        g.Players.Any(p => p.UserId != null && p.UserId.Value == request.UserId))
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
            .ToDictionary(u => u.Id.Value, u => u.Nickname.Value);

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
            g.StartedAt,
            g.Difficulty.Value,
            g.Result is not null,
            g.Result?.Win,
            g.Result?.Score.Value,
            g.Players.Count,
            g.PlayedAdversaries.Select(a => a.AdversaryId.Value).ToList(),
            g.Scenario?.ScenarioId.Value,
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
