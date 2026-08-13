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
/// </summary>
public sealed record SetupFactSeat(string SpiritId, string BoardId, bool IsMine, Guid? UserId, Guid? PlayerId);

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
                    p.PlayerId?.Value))
                .ToList())).ToList();
    }
}
