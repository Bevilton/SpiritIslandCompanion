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
    List<string> SpiritIds);

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
            g.Players.Select(p => p.SpiritId.Value).ToList())).ToList();

        return response;
    }
}
