using Domain.Models.Friendship;
using Domain.Models.User;

namespace Application.Features.Friendships;

/// <summary>
/// Query shapes shared by every reader of friendships. A friendship has two sides and either
/// may have asked, so "involving this user" and "between these two" are each two conditions —
/// spelled out here once rather than at every handler that needs them.
/// </summary>
internal static class FriendshipQueries
{
    /// <summary>Friendships with the user on either side.</summary>
    public static IQueryable<Friendship> Involving(this IQueryable<Friendship> friendships, UserId userId) =>
        friendships.Where(f => f.RequesterId == userId || f.AddresseeId == userId);

    /// <summary>The friendship between two users, whichever of them asked.</summary>
    public static IQueryable<Friendship> Between(this IQueryable<Friendship> friendships, UserId one, UserId other) =>
        friendships.Where(f =>
            (f.RequesterId == one && f.AddresseeId == other) ||
            (f.RequesterId == other && f.AddresseeId == one));

    public static IQueryable<Friendship> Accepted(this IQueryable<Friendship> friendships) =>
        friendships.Where(f => f.Status == FriendshipStatus.Accepted);

    public static IQueryable<Friendship> Pending(this IQueryable<Friendship> friendships) =>
        friendships.Where(f => f.Status == FriendshipStatus.Pending);
}
