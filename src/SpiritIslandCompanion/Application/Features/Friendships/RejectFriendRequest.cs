using Application.Abstractions;
using Application.Data;
using Domain.Errors;
using Domain.Models.Friendship;
using Domain.Models.User;
using Domain.Results;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Friendships;

/// <summary>
/// Rejects a pending friend request. Only the addressee can reject.
/// </summary>
public sealed record RejectFriendRequestCommand(Guid FriendshipId, Guid CurrentUserId) : ICommand;

internal sealed class RejectFriendRequestHandler(IAppDbContext db) : ICommandHandler<RejectFriendRequestCommand>
{
    public async Task<Result> Handle(RejectFriendRequestCommand request, CancellationToken cancellationToken)
    {
        var friendship = await db.Friendships
            .FirstOrDefaultAsync(f => f.Id == new FriendshipId(request.FriendshipId), cancellationToken);

        if (friendship is null)
            return Result.Failure(DomainErrors.Friendship.NotFound);

        if (friendship.AddresseeId != new UserId(request.CurrentUserId))
            return Result.Failure(Error.Forbidden("Friendship.NotAddressee", "Only the recipient can reject a friend request."));

        return friendship.Reject();
    }
}
