using Application.Abstractions;
using Application.Data;
using Domain.Errors;
using Domain.Models.Friendship;
using Domain.Models.User;
using Domain.Results;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Friendships;

/// <summary>
/// Declines a pending friend request. Only the addressee can. The request is removed rather
/// than marked: nothing ever reads a declined request back, and the requester may ask again
/// later — the same reasoning as <see cref="PlayerMerges.WithdrawMergeRequestCommand"/>.
/// </summary>
public sealed record RejectFriendRequestCommand(Guid FriendshipId, Guid UserId) : ICommand;

internal sealed class RejectFriendRequestHandler(IAppDbContext db) : ICommandHandler<RejectFriendRequestCommand>
{
    public async Task<Result> Handle(RejectFriendRequestCommand request, CancellationToken cancellationToken)
    {
        var friendship = await db.Friendships
            .FirstOrDefaultAsync(f => f.Id == new FriendshipId(request.FriendshipId), cancellationToken);

        if (friendship is null)
            return Result.Failure(DomainErrors.Friendship.NotFound);

        // Whether this user may answer, and whether there is still anything to answer, are the
        // friendship's own rules.
        var open = friendship.AwaitingAnswerFrom(new UserId(request.UserId));
        if (open.IsFailure)
            return open;

        db.Friendships.Remove(friendship);
        return Result.Success();
    }
}
