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
public sealed record AcceptFriendRequestCommand(Guid FriendshipId, Guid UserId) : ICommand;

internal sealed class AcceptFriendRequestHandler(IAppDbContext db, TimeProvider clock) : ICommandHandler<AcceptFriendRequestCommand>
{
    public async Task<Result> Handle(AcceptFriendRequestCommand request, CancellationToken cancellationToken)
    {
        var friendship = await db.Friendships
            .FirstOrDefaultAsync(f => f.Id == new FriendshipId(request.FriendshipId), cancellationToken);

        if (friendship is null)
            return Result.Failure(DomainErrors.Friendship.NotFound);

        // Whether this user may answer, and whether there is still anything to answer, are the
        // friendship's own rules.
        return friendship.Accept(new UserId(request.UserId), clock.GetUtcNow());
    }
}
