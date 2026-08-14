using Application.Abstractions;
using Application.Data;
using Application.Features.Games;
using Domain.Results;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Statistics;

/// <summary>
/// Compact per-game history for the requesting user, consumed by the game-setup
/// screen. It is loaded once and the matchup insights (record vs an adversary /
/// scenario, spirit coverage, board affinity) are computed in-memory on every
/// selection change — no extra round-trip while the user browses the setup.
/// </summary>
public sealed record GetSetupFactsQuery(Guid UserId) : IQuery<List<SetupGameFact>>;

public sealed record SetupFactAdversary(string AdversaryId, int Level);

/// <summary>
/// One seat at the table. <see cref="IsMine"/> marks the requesting user's seat;
/// <see cref="UserId"/>/<see cref="PlayerId"/> identify a friend or local player
/// so seat stats can be scoped to whoever will sit there.
/// <see cref="PlayerName"/> is the display name behind that identity (user
/// nickname or local player name; null for an unassigned seat) — the record
/// sheets use it to say who played what.
/// </summary>
public sealed record SetupFactSeat(
    string SpiritId,
    string BoardId,
    bool IsMine,
    Guid? UserId,
    Guid? PlayerId,
    string? PlayerName = null);

/// <param name="CustomLayoutId">
/// The player's saved layout the island came from, for games built by hand out of one.
/// </param>
public sealed record SetupGameFact(
    Guid GameId,
    DateTimeOffset StartedAt,
    bool IsCompleted,
    bool? Win,
    int? Score,
    int Difficulty,
    string IslandSetupId,
    Guid? CustomLayoutId,
    string? ScenarioId,
    List<SetupFactAdversary> Adversaries,
    List<SetupFactSeat> Seats);

internal sealed class GetSetupFactsHandler(IAppDbContext db) : IQueryHandler<GetSetupFactsQuery, List<SetupGameFact>>
{
    public async Task<Result<List<SetupGameFact>>> Handle(GetSetupFactsQuery request, CancellationToken cancellationToken)
    {
        var games = await db.Games
            .InvolvingUser(request.UserId)
            .OrderByDescending(g => g.StartedAt)
            .ToListAsync(cancellationToken);

        // Resolve display names for every user / player sitting in any seat, the same
        // way GetStatistics does — the value-object types go in directly so EF Core's
        // configured HasConversion translates Contains into a SQL IN clause.
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

        return games.Select(g => new SetupGameFact(
            g.Id.Value,
            g.StartedAt,
            g.Result is not null,
            g.Result?.Win,
            g.Result?.Score.Value,
            g.Difficulty.Value,
            g.IslandSetupId.Value,
            g.CustomLayoutId?.Value,
            g.Scenario?.ScenarioId.Value,
            g.PlayedAdversaries
                .Select(a => new SetupFactAdversary(a.AdversaryId.Value, a.Level.Value))
                .ToList(),
            g.Players
                .Select(p => new SetupFactSeat(
                    p.SpiritId.Value,
                    p.StartingBoard.Value,
                    p.UserId != null && p.UserId.Value == request.UserId,
                    p.UserId?.Value,
                    p.PlayerId?.Value,
                    p.UserId is not null ? userLookup.GetValueOrDefault(p.UserId.Value)
                        : p.PlayerId is not null ? playerLookup.GetValueOrDefault(p.PlayerId.Value)
                        : null))
                .ToList())).ToList();
    }
}
