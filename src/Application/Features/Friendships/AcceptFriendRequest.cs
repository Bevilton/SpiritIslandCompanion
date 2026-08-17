using Application.Abstractions;
using Application.Data;
using Domain.Errors;
using Domain.Models.Friendship;
using Domain.Models.User;
using Domain.Results;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Friendships;

/// <summary>
/// Accepts a pending friend request. Only the addressee can accept.
/// </summary>
public sealed record AcceptFriendRequestCommand(Guid FriendshipId, Guid CurrentUserId) : ICommand;

internal sealed class AcceptFriendRequestHandler(IAppDbContext db) : ICommandHandler<AcceptFriendRequestCommand>
{
    public async Task<Result> Handle(AcceptFriendRequestCommand request, CancellationToken cancellationToken)
    {
        var friendship = await db.Friendships
            .FirstOrDefaultAsync(f => f.Id == new FriendshipId(request.FriendshipId), cancellationToken);

        if (friendship is null)
            return Result.Failure(DomainErrors.Friendship.NotFound);

        if (friendship.AddresseeId != new UserId(request.CurrentUserId))
            return Result.Failure(Error.Forbidden("Friendship.NotAddressee", "Only the recipient can accept a friend request."));

        return friendship.Accept();
    }
}
