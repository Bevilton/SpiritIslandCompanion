using Domain.Models.Player;
using Domain.Models.PlayerMerge;
using Domain.Models.User;

namespace Application.Features.PlayerMerges;

/// <summary>
/// Query shapes shared by every reader of merge requests. A request has two sides — asking,
/// and asked — so "involving this user" and "between these two" are each two conditions,
/// spelled out here once rather than at every handler that needs them.
/// </summary>
internal static class PlayerMergeRequestQueries
{
    /// <summary>Requests with the user on either side — asking, or asked.</summary>
    public static IQueryable<PlayerMergeRequest> Involving(this IQueryable<PlayerMergeRequest> requests, UserId userId) =>
        requests.Where(r => r.RequesterId == userId || r.TargetUserId == userId);

    /// <summary>Requests between two users, whichever of them asked.</summary>
    public static IQueryable<PlayerMergeRequest> Between(this IQueryable<PlayerMergeRequest> requests, UserId one, UserId other) =>
        requests.Where(r =>
            (r.RequesterId == one && r.TargetUserId == other) ||
            (r.RequesterId == other && r.TargetUserId == one));

    /// <summary>Requests about one local player, whatever their state.</summary>
    public static IQueryable<PlayerMergeRequest> About(this IQueryable<PlayerMergeRequest> requests, PlayerId playerId) =>
        requests.Where(r => r.PlayerId == playerId);

    public static IQueryable<PlayerMergeRequest> Pending(this IQueryable<PlayerMergeRequest> requests) =>
        requests.Where(r => r.Status == PlayerMergeStatus.Pending);
}
