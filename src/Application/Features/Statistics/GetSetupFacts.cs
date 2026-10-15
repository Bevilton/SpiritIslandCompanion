using Application.Abstractions;
using Application.Data;
using Application.Features.Games;
using Application.Features.Games.Dtos;
using Domain.Models.User;
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

/// <summary>
/// One seat at the table. <see cref="IsMine"/> marks the requesting user's seat;
/// <see cref="UserId"/>/<see cref="PlayerId"/> identify a friend or local player
/// so seat stats can be scoped to whoever will sit there.
/// <see cref="PlayerName"/> is the display name behind that identity (user
/// nickname or local player name; null for an unassigned seat) — the record
/// sheets use it to say who played what.
/// <see cref="AspectId"/> is the variant of the spirit that was played, null for
/// the spirit as printed; an aspect changes the spirit enough that its record is
/// worth counting separately.
/// </summary>
public sealed record SetupFactSeat(
    string SpiritId,
    string? AspectId,
    string BoardId,
    bool IsMine,
    Guid? UserId,
    Guid? PlayerId,
    string? PlayerName);

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
    List<GameAdversaryResponse> Adversaries,
    List<SetupFactSeat> Seats);

internal sealed class GetSetupFactsHandler(IAppDbContext db, SeatNameResolver seatNames) : IQueryHandler<GetSetupFactsQuery, List<SetupGameFact>>
{
    public async Task<Result<List<SetupGameFact>>> Handle(GetSetupFactsQuery request, CancellationToken cancellationToken)
    {
        var userId = new UserId(request.UserId);
        var games = await db.Games
            .InvolvingUser(userId)
            .OrderByDescending(g => g.StartedAt)
            .ToListAsync(cancellationToken);

        var names = await seatNames.LoadAsync(games, cancellationToken);

        return games.Select(g => new SetupGameFact(
            g.Id.Value,
            g.StartedAt,
            g.Result is not null,
            g.Result?.Outcome.Win,
            g.Result?.Score.Value,
            g.Difficulty.Value,
            g.IslandSetupId.Value,
            g.CustomLayoutId?.Value,
            g.ScenarioId?.Value,
            g.PlayedAdversaries.Select(a => a.ToResponse()).ToList(),
            g.Players
                .Select(p => new SetupFactSeat(
                    p.SpiritId.Value,
                    p.AspectId?.Value,
                    p.StartingBoard.Value,
                    p.UserId == userId,
                    p.UserId?.Value,
                    p.PlayerId?.Value,
                    p.UserId is not null ? names.Users.GetValueOrDefault(p.UserId.Value)
                        : p.PlayerId is not null ? names.Players.GetValueOrDefault(p.PlayerId.Value)
                        : null))
                .ToList())).ToList();
    }
}
