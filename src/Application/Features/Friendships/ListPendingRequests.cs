using Application.Abstractions;
using Application.Data;
using Application.Features.Games;
using Domain.Models.Friendship;
using Domain.Models.User;
using Domain.Results;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Friendships;

public sealed record ListPendingRequestsQuery(Guid UserId) : IQuery<PendingRequestsResponse>;

public sealed record PendingRequestsResponse(
    List<PendingFriendRequest> Incoming,
    List<PendingFriendRequest> Outgoing);

/// <summary>
/// A friend request still waiting on an answer, read from one side of it:
/// <paramref name="UserId"/> is the other person, whichever of the two asked. Shared by the
/// dashboard's nudge and the People page, so the two can't disagree.
/// </summary>
public sealed record PendingFriendRequest(
    Guid FriendshipId,
    Guid UserId,
    string Name,
    string Email,
    DateTimeOffset SentAt);

internal sealed class ListPendingRequestsHandler(IAppDbContext db) : IQueryHandler<ListPendingRequestsQuery, PendingRequestsResponse>
{
    public async Task<Result<PendingRequestsResponse>> Handle(ListPendingRequestsQuery request, CancellationToken cancellationToken)
    {
        var userId = new UserId(request.UserId);

        var pending = await db.Friendships
            .AsNoTracking()
            .Pending()
            .Involving(userId)
            .ToListAsync(cancellationToken);

        var otherUserIds = pending
            .Select(f => f.GetOtherUserId(userId))
            .Distinct()
            .ToList();

        var users = await db.Users
            .AsNoTracking()
            .Where(u => otherUserIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        var (incoming, outgoing) = PendingFriendRequests.Split(pending, userId, users);
        return new PendingRequestsResponse(incoming, outgoing);
    }
}

/// <summary>
/// Pending friendships as they read from one side: the ones waiting on this user, and the
/// ones this user is waiting on. The dashboard's nudge and the People page both list them,
/// from friendships each loaded its own way — the shaping lives here so the two can't disagree.
/// </summary>
internal static class PendingFriendRequests
{
    public static (List<PendingFriendRequest> Incoming, List<PendingFriendRequest> Outgoing) Split(
        IReadOnlyCollection<Friendship> pending, UserId userId, IReadOnlyDictionary<UserId, User> users)
    {
        PendingFriendRequest Describe(Friendship friendship, UserId otherUserId)
        {
            var other = users.GetValueOrDefault(otherUserId);
            return new PendingFriendRequest(
                friendship.Id.Value,
                otherUserId.Value,
                other?.DisplayName ?? SeatNames.UnknownName,
                other?.Email.Value ?? "",
                friendship.CreatedAt);
        }

        var incoming = pending
            .Where(f => f.AddresseeId == userId)
            .Select(f => Describe(f, f.RequesterId))
            .OrderByDescending(r => r.SentAt)
            .ToList();

        var outgoing = pending
            .Where(f => f.RequesterId == userId)
            .Select(f => Describe(f, f.AddresseeId))
            .OrderByDescending(r => r.SentAt)
            .ToList();

        return (incoming, outgoing);
    }
}
