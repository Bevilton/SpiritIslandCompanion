using Application.Abstractions;
using Application.Data;
using Domain.Errors;
using Domain.Models.Friendship;
using Domain.Models.User;
using Domain.Results;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Friendships;

/// <summary>
/// Ends a friendship. Either party can, and it takes effect for both.
/// <para>
/// The games they played together are not deleted and not left dangling: each side comes out
/// owning their own record of them, with the other person kept as a local player. See
/// <see cref="SharedGameSplitter"/> for what that means seat by seat.
/// </para>
/// </summary>
public sealed record RemoveFriendCommand(Guid FriendshipId, Guid CurrentUserId) : ICommand;

internal sealed class RemoveFriendHandler(IAppDbContext db) : ICommandHandler<RemoveFriendCommand>
{
    public async Task<Result> Handle(RemoveFriendCommand request, CancellationToken cancellationToken)
    {
        var friendship = await db.Friendships
            .FirstOrDefaultAsync(f => f.Id == new FriendshipId(request.FriendshipId), cancellationToken);

        if (friendship is null)
            return Result.Failure(DomainErrors.Friendship.NotFound);

        var currentUserId = new UserId(request.CurrentUserId);
        if (!friendship.InvolvesUser(currentUserId))
            return Result.Failure(Error.Forbidden("Friendship.NotInvolved", "You are not part of this friendship."));

        var otherUserId = friendship.GetOtherUserId(currentUserId);

        // Only an accepted friendship can have put the two of them at a table together; a
        // request that was never answered has nothing to unpick.
        if (friendship.Status == FriendshipStatus.Accepted)
            await new SharedGameSplitter(db).SplitAsync(currentUserId, otherUserId, cancellationToken);

        // A merge in flight between them asks one to take over seats in the other's games —
        // exactly the link being severed here.
        var merges = await db.PlayerMergeRequests
            .Where(m =>
                (m.RequesterId.Value == request.CurrentUserId && m.TargetUserId.Value == otherUserId.Value) ||
                (m.RequesterId.Value == otherUserId.Value && m.TargetUserId.Value == request.CurrentUserId))
            .ToListAsync(cancellationToken);

        foreach (var merge in merges)
            db.PlayerMergeRequests.Remove(merge);

        db.Friendships.Remove(friendship);
        return Result.Success();
    }
}
