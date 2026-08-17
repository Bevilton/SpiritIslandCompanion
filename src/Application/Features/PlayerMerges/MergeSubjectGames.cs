using Application.Data;
using Application.Features.Games.Dtos;
using Domain.Models.Game;
using Domain.Models.Player;
using Domain.Models.User;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.PlayerMerges;

/// <summary>
/// One game a merge would move: what was played, and the seat that would change hands.
/// The approver is not otherwise allowed to see these games, so this carries the setup and
/// outcome and nothing about the other people at the table beyond how many there were.
/// </summary>
public sealed record MergeGameSummary(
    DateTimeOffset StartedAt,
    bool IsCompleted,
    bool? Win,
    int? Score,
    int Difficulty,
    int PlayerCount,
    string SpiritId,
    string? AspectId,
    string BoardId,
    List<GameAdversaryResponse> Adversaries,
    string? ScenarioId);

/// <summary>The games a local player sits in — the subject of a merge, and what it rewrites.</summary>
internal static class MergeSubjectGames
{
    /// <summary>
    /// Every game seating the local player. Not filtered by owner: the seats are what the
    /// merge rewrites, so the set has to be exactly the seats that exist.
    /// </summary>
    public static Task<List<Game>> LoadAsync(
        IAppDbContext db, PlayerId playerId, bool tracked, CancellationToken cancellationToken)
    {
        // The seat's PlayerId is an owned type, not a converted key — compare the Guid inside
        // it, the way every other reader of a seat does (see GameQueries.InvolvingUser).
        var value = playerId.Value;
        var games = tracked ? db.Games : db.Games.AsNoTracking();
        return games
            .Where(g => g.Players.Any(p => p.PlayerId != null && p.PlayerId.Value == value))
            .OrderByDescending(g => g.StartedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// True when merging would seat the target twice at one table. The app never offers a
    /// game like that, but a local player standing in for someone already seated under their
    /// own account is exactly the mistake a merge would silently bake in.
    /// </summary>
    public static bool HasSeatConflict(IEnumerable<Game> games, PlayerId playerId, UserId targetUserId) =>
        games.Any(g => g.Players.Any(p => p.PlayerId == playerId)
                       && g.Players.Any(p => p.UserId is not null && p.UserId == targetUserId));

    public static List<MergeGameSummary> Summarise(IEnumerable<Game> games, PlayerId playerId) =>
        games
            .Select(g =>
            {
                var seat = g.Players.First(p => p.PlayerId == playerId);
                return new MergeGameSummary(
                    g.StartedAt,
                    g.Result is not null,
                    g.Result?.Win,
                    g.Result?.Score.Value,
                    g.Difficulty.Value,
                    g.Players.Count,
                    seat.SpiritId.Value,
                    seat.AspectId?.Value,
                    seat.StartingBoard.Value,
                    g.PlayedAdversaries
                        .Select(a => new GameAdversaryResponse(a.AdversaryId.Value, a.Level.Value))
                        .ToList(),
                    g.Scenario?.ScenarioId.Value);
            })
            .ToList();
}
