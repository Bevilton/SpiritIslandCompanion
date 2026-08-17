using Application.Abstractions;
using Application.Data;
using Domain.Models.PlayerMerge;
using Domain.Results;
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
/// The merges waiting on this user to answer. Small on purpose: the dashboard asks for it
/// alongside friend requests, and both are nudges rather than the page that owns them.
/// </summary>
public sealed record ListIncomingMergeRequestsQuery(Guid UserId) : IQuery<List<MergeRequestSummary>>;

internal sealed class ListIncomingMergeRequestsHandler(IAppDbContext db)
    : IQueryHandler<ListIncomingMergeRequestsQuery, List<MergeRequestSummary>>
{
    public async Task<Result<List<MergeRequestSummary>>> Handle(
        ListIncomingMergeRequestsQuery request, CancellationToken cancellationToken) =>
        await MergeRequestSummaries.IncomingAsync(db, request.UserId, cancellationToken);
}

/// <summary>Shared by the dashboard's nudge and the People page's inbox, so they can't disagree.</summary>
internal static class MergeRequestSummaries
{
    public static async Task<List<MergeRequestSummary>> IncomingAsync(
        IAppDbContext db, Guid userId, CancellationToken cancellationToken)
    {
        var merges = await db.PlayerMergeRequests
            .AsNoTracking()
            .Where(r => r.TargetUserId.Value == userId && r.Status == PlayerMergeStatus.Pending)
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

        // How many seats the merge would move — a fact about the requester's games, counted
        // over theirs rather than over anything the asking user can see. Counted in the
        // database: a nudge on the dashboard has no business dragging whole game aggregates,
        // island geometry and all, across for a number. One query per guest named in the
        // inbox, which is a handful at the very most.
        var gameCounts = new Dictionary<Guid, int>();
        foreach (var playerId in playerIds)
        {
            var value = playerId.Value;
            gameCounts[value] = await db.Games
                .AsNoTracking()
                .CountAsync(g => g.Players.Any(p => p.PlayerId != null && p.PlayerId.Value == value),
                    cancellationToken);
        }

        return merges
            .Select(m => new MergeRequestSummary(
                m.Id.Value,
                playerNames.GetValueOrDefault(m.PlayerId.Value, "Unknown"),
                requesterNames.GetValueOrDefault(m.RequesterId.Value, "Unknown"),
                m.Status,
                gameCounts.GetValueOrDefault(m.PlayerId.Value),
                m.CreatedAt))
            .ToList();
    }
}
