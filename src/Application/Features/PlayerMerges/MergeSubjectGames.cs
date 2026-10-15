using Application.Data;
using Application.Features.Games;
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
internal sealed class MergeSubjectGames(IAppDbContext db)
{
    /// <summary>
    /// What a merge's local player is called once the merge has removed them: the request
    /// still has to read as a sentence about someone afterwards.
    /// </summary>
    public const string MergedPlayerName = "Merged player";

    /// <summary>
    /// Every game seating the local player. Not filtered by owner: the seats are what the
    /// merge rewrites, so the set has to be exactly the seats that exist.
    /// </summary>
    public Task<List<Game>> LoadAsync(PlayerId playerId, bool tracked, CancellationToken cancellationToken)
    {
        var games = tracked ? db.Games : db.Games.AsNoTracking();
        return games
            .SeatingLocalPlayer(playerId)
            .OrderByDescending(g => g.StartedAt)
            .ToListAsync(cancellationToken);
    }

    public static List<MergeGameSummary> Summarise(IEnumerable<Game> games, PlayerId playerId) =>
        games
            .Select(g =>
            {
                var seat = g.Players.First(p => p.PlayerId == playerId);
                return new MergeGameSummary(
                    g.StartedAt,
                    g.Result is not null,
                    g.Result?.Outcome.Win,
                    g.Result?.Score.Value,
                    g.Difficulty.Value,
                    g.Players.Count,
                    seat.SpiritId.Value,
                    seat.AspectId?.Value,
                    seat.StartingBoard.Value,
                    g.PlayedAdversaries.Select(a => a.ToResponse()).ToList(),
                    g.ScenarioId?.Value);
            })
            .ToList();
}
