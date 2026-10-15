using Application.Data;
using Application.Features.Games;
using Domain.Models.PlayerMerge;
using Domain.Models.User;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.PlayerMerges;

/// <summary>
/// A merge waiting on somebody's answer, as a list row: who is asking, which guest of theirs
/// they say is you, and how many seats it covers. The seats themselves are in
/// <see cref="GetMergeRequestQuery"/> — nobody should agree from a summary.
/// </summary>
public sealed record MergeRequestSummary(
    Guid Id,
    string PlayerName,
    string OtherUserName,
    PlayerMergeStatus Status,
    int GameCount,
    DateTimeOffset CreatedAt);

/// <summary>
/// The merges waiting on a user to answer. Shared by the dashboard's nudge and the People
/// page's inbox, so the two can't disagree.
/// </summary>
internal sealed class MergeRequestInbox(IAppDbContext db)
{
    public async Task<List<MergeRequestSummary>> ListAsync(UserId target, CancellationToken cancellationToken)
    {
        var merges = await db.PlayerMergeRequests
            .AsNoTracking()
            .Where(r => r.TargetUserId == target)
            .Pending()
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        if (merges.Count == 0) return [];

        // The local player belongs to the requester's account, so its name has to be fetched
        // rather than read off anything of ours.
        var playerIds = merges.Select(m => m.PlayerId).Distinct().ToList();
        var playerNames = (await db.Players
                .AsNoTracking()
                .Where(p => playerIds.Contains(p.Id))
                .ToListAsync(cancellationToken))
            .ToDictionary(p => p.Id.Value, p => p.Name.Value);

        var requesterIds = merges.Select(m => m.RequesterId).Distinct().ToList();
        var requesterNames = (await db.Users
                .AsNoTracking()
                .Where(u => requesterIds.Contains(u.Id))
                .ToListAsync(cancellationToken))
            .ToDictionary(u => u.Id.Value, u => u.DisplayName);

        // How many games the merge would move — a fact about the requester's games, counted
        // over theirs rather than over anything the asking user can see, and counted in the
        // database: a nudge on the dashboard has no business dragging whole game aggregates,
        // island geometry and all, across for a number. Per game rather than per seat, since a
        // guest who played two spirits one evening still sat at one table. The seats are joined
        // flat and filtered afterwards: a filter inside the collection would need SQL's APPLY,
        // which the SQLite behind the demo does not have.
        var gameCounts = await db.Games
            .AsNoTracking()
            .SelectMany(g => g.Players, (g, seat) => new { seat.PlayerId, GameId = g.Id })
            .Where(seat => seat.PlayerId != null && playerIds.Contains(seat.PlayerId))
            .GroupBy(seat => seat.PlayerId!)
            .Select(seats => new { PlayerId = seats.Key, Games = seats.Select(s => s.GameId).Distinct().Count() })
            .ToDictionaryAsync(x => x.PlayerId.Value, x => x.Games, cancellationToken);

        return merges
            .Select(m => new MergeRequestSummary(
                m.Id.Value,
                playerNames.GetValueOrDefault(m.PlayerId.Value, SeatNames.UnknownName),
                requesterNames.GetValueOrDefault(m.RequesterId.Value, SeatNames.UnknownName),
                m.Status,
                gameCounts.GetValueOrDefault(m.PlayerId.Value),
                m.CreatedAt))
            .ToList();
    }
}
