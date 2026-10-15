using Application.Abstractions;
using Application.Data;
using Domain.Models.User;
using Domain.Results;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Friendships;

public sealed record ListFriendsQuery(Guid UserId) : IQuery<List<FriendResponse>>;

/// <param name="Name">
/// What the friend is called — their nickname, or their email until they pick one.
/// </param>
public sealed record FriendResponse(Guid FriendshipId, Guid FriendUserId, string Email, string Name);

internal sealed class ListFriendsHandler(IAppDbContext db) : IQueryHandler<ListFriendsQuery, List<FriendResponse>>
{
    public async Task<Result<List<FriendResponse>>> Handle(ListFriendsQuery request, CancellationToken cancellationToken)
    {
        var userId = new UserId(request.UserId);

        var friendships = await db.Friendships
            .AsNoTracking()
            .Accepted()
            .Involving(userId)
            .ToListAsync(cancellationToken);

        var friendIds = friendships.Select(f => f.GetOtherUserId(userId)).ToList();

        var friends = await db.Users
            .AsNoTracking()
            .Where(u => friendIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        var response = friendships
            .Select(f => (Friendship: f, Friend: friends.GetValueOrDefault(f.GetOtherUserId(userId))))
            .Where(x => x.Friend is not null)
            .Select(x => new FriendResponse(
                x.Friendship.Id.Value,
                x.Friend!.Id.Value,
                x.Friend.Email.Value,
                x.Friend.DisplayName))
            .OrderBy(f => f.Name)
            .ToList();

        return response;
    }
}
