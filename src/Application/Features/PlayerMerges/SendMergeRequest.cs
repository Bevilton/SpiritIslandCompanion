using Application.Abstractions;
using Application.Data;
using Domain.Errors;
using Domain.Models.Friendship;
using Domain.Models.Player;
using Domain.Models.PlayerMerge;
using Domain.Models.User;
using Domain.Results;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.PlayerMerges;

/// <summary>
/// Asks a friend to take over a local player's seats — "this guest in my games is you".
/// Nothing moves until they approve; see <see cref="PlayerMergeRequest"/>.
/// </summary>
public sealed record SendMergeRequestCommand(Guid RequesterId, Guid PlayerId, Guid TargetUserId) : ICommand;

internal sealed class SendMergeRequestHandler(IAppDbContext db) : ICommandHandler<SendMergeRequestCommand>
{
    public async Task<Result> Handle(SendMergeRequestCommand request, CancellationToken cancellationToken)
    {
        var requesterId = new UserId(request.RequesterId);
        var targetId = new UserId(request.TargetUserId);
        var playerId = new PlayerId(request.PlayerId);

        var player = await db.Players
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == playerId, cancellationToken);

        if (player is null)
            return Result.Failure(DomainErrors.Player.NotFound);
        if (player.CreatedBy != requesterId)
            return Result.Failure(DomainErrors.Player.NotYours);

        // Only a friend can be asked: the merge hands them the seats and, with them, a place
        // in each other's game history — the same bar CreateGame sets for seating an account.
        var areFriends = await db.Friendships
            .AsNoTracking()
            .AnyAsync(f => f.Status == FriendshipStatus.Accepted &&
                           ((f.RequesterId.Value == request.RequesterId && f.AddresseeId.Value == request.TargetUserId) ||
                            (f.RequesterId.Value == request.TargetUserId && f.AddresseeId.Value == request.RequesterId)),
                cancellationToken);

        if (!areFriends)
            return Result.Failure(DomainErrors.Friendship.NotAccepted);

        var pending = await db.PlayerMergeRequests
            .AsNoTracking()
            .AnyAsync(r => r.Status == PlayerMergeStatus.Pending && r.PlayerId.Value == request.PlayerId,
                cancellationToken);

        if (pending)
            return Result.Failure(DomainErrors.PlayerMerge.AlreadyPending);

        // Caught here as well as on approval, so the person asking is the one who finds out.
        var games = await MergeSubjectGames.LoadAsync(db, playerId, tracked: false, cancellationToken);
        if (MergeSubjectGames.HasSeatConflict(games, playerId, targetId))
            return Result.Failure(DomainErrors.PlayerMerge.SeatConflict);

        var mergeResult = PlayerMergeRequest.Create(
            new PlayerMergeRequestId(Guid.NewGuid()), playerId, requesterId, targetId);

        if (mergeResult.IsFailure)
            return Result.Failure(mergeResult.Error);

        db.PlayerMergeRequests.Add(mergeResult.Value);
        return Result.Success();
    }
}
