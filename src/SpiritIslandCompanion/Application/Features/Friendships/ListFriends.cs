using Application.Abstractions;
using Application.Data;
using Domain.Models.Friendship;
using Domain.Models.User;
using Domain.Results;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Friendships;

public sealed record ListFriendsQuery(Guid UserId) : IQuery<List<FriendResponse>>;

public sealed record FriendResponse(Guid FriendshipId, Guid FriendUserId, string Email, string Nickname);

internal sealed class ListFriendsHandler(IAppDbContext db) : IQueryHandler<ListFriendsQuery, List<FriendResponse>>
{
    public async Task<Result<List<FriendResponse>>> Handle(ListFriendsQuery request, CancellationToken cancellationToken)
    {
        var userId = new UserId(request.UserId);

        var friendships = await db.Friendships
            .AsNoTracking()
            .Where(f => f.Status == FriendshipStatus.Accepted &&
                        (f.RequesterId == userId || f.AddresseeId == userId))
            .ToListAsync(cancellationToken);

        var friendUserIds = friendships
            .Select(f => f.GetOtherUserId(userId))
            .ToList();

        var users = await db.Users
            .AsNoTracking()
            .Where(u => friendUserIds.Contains(u.Id))
            .ToListAsync(cancellationToken);

        var userLookup = users.ToDictionary(u => u.Id);

        var response = friendships
            .Where(f => userLookup.ContainsKey(f.GetOtherUserId(userId)))
            .Select(f =>
            {
                var friendUser = userLookup[f.GetOtherUserId(userId)];
                return new FriendResponse(
                    f.Id.Value,
                    friendUser.Id.Value,
                    friendUser.Email.Value,
                    friendUser.Nickname.Value);
            })
            .OrderBy(f => f.Nickname)
            .ToList();

        return response;
    }
}
