using Application.Abstractions;
using Application.Data;
using Domain.Errors;
using Domain.Models.Friendship;
using Domain.Models.User;
using Domain.Results;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Friendships;

/// <summary>
/// Removes an accepted friendship. Either party can remove.
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

        if (!friendship.InvolvesUser(new UserId(request.CurrentUserId)))
            return Result.Failure(Error.Forbidden("Friendship.NotInvolved", "You are not part of this friendship."));

        db.Friendships.Remove(friendship);
        return Result.Success();
    }
}
