using Application.Abstractions;
using Application.Data;
using Domain.Errors;
using Domain.Models.PlayerMerge;
using Domain.Models.User;
using Domain.Results;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.PlayerMerges;

/// <summary>
/// Accepts the merge: every seat the local player holds becomes a seat of the approving
/// account, and the local player — now standing for nobody — is removed.
/// <para>
/// Only the target can approve. The games belong to the requester and stay theirs; what
/// changes is who is recorded as having played, which is precisely what makes them show up
/// in the approver's own history from here on.
/// </para>
/// </summary>
public sealed record ApproveMergeRequestCommand(Guid RequestId, Guid CurrentUserId) : ICommand;

internal sealed class ApproveMergeRequestHandler(IAppDbContext db) : ICommandHandler<ApproveMergeRequestCommand>
{
    public async Task<Result> Handle(ApproveMergeRequestCommand request, CancellationToken cancellationToken)
    {
        var merge = await db.PlayerMergeRequests
            .FirstOrDefaultAsync(r => r.Id == new PlayerMergeRequestId(request.RequestId), cancellationToken);

        if (merge is null)
            return Result.Failure(DomainErrors.PlayerMerge.NotFound);
        if (merge.TargetUserId != new UserId(request.CurrentUserId))
            return Result.Failure(DomainErrors.PlayerMerge.NotTarget);
        if (merge.Status != PlayerMergeStatus.Pending)
            return Result.Failure(DomainErrors.PlayerMerge.AlreadyResponded);

        var games = await MergeSubjectGames.LoadAsync(db, merge.PlayerId, tracked: true, cancellationToken);

        // Re-checked at the moment it would take effect: the games may have moved on since
        // the request was sent.
        if (MergeSubjectGames.HasSeatConflict(games, merge.PlayerId, merge.TargetUserId))
            return Result.Failure(DomainErrors.PlayerMerge.SeatConflict);

        foreach (var seat in games.SelectMany(g => g.Players).Where(p => p.PlayerId == merge.PlayerId))
            seat.ReassignToUser(merge.TargetUserId);

        var player = await db.Players.FirstOrDefaultAsync(p => p.Id == merge.PlayerId, cancellationToken);
        if (player is not null)
            db.Players.Remove(player);

        // Any other request naming this guest goes with them: an earlier attempt that came
        // back declined would otherwise sit in the requester's outbox for good, pointing at
        // somebody who no longer exists.
        var playerId = merge.PlayerId.Value;
        var superseded = await db.PlayerMergeRequests
            .Where(r => r.PlayerId.Value == playerId && r.Id != merge.Id)
            .ToListAsync(cancellationToken);

        foreach (var stale in superseded)
            db.PlayerMergeRequests.Remove(stale);

        return merge.Approve();
    }
}

/// <summary>Declines the merge — "that guest wasn't me". Only the target can.</summary>
public sealed record RejectMergeRequestCommand(Guid RequestId, Guid CurrentUserId) : ICommand;

internal sealed class RejectMergeRequestHandler(IAppDbContext db) : ICommandHandler<RejectMergeRequestCommand>
{
    public async Task<Result> Handle(RejectMergeRequestCommand request, CancellationToken cancellationToken)
    {
        var merge = await db.PlayerMergeRequests
            .FirstOrDefaultAsync(r => r.Id == new PlayerMergeRequestId(request.RequestId), cancellationToken);

        if (merge is null)
            return Result.Failure(DomainErrors.PlayerMerge.NotFound);
        if (merge.TargetUserId != new UserId(request.CurrentUserId))
            return Result.Failure(DomainErrors.PlayerMerge.NotTarget);

        return merge.Reject();
    }
}

/// <summary>
/// Clears the requester's own request off the board — taking back one still waiting, or
/// acknowledging one that came back declined. Both are the same act from where the
/// requester sits, and neither leaves anything worth keeping: an approved merge has already
/// rewritten the seats, and nothing refers back to the request afterwards.
/// </summary>
public sealed record WithdrawMergeRequestCommand(Guid RequestId, Guid CurrentUserId) : ICommand;

internal sealed class WithdrawMergeRequestHandler(IAppDbContext db) : ICommandHandler<WithdrawMergeRequestCommand>
{
    public async Task<Result> Handle(WithdrawMergeRequestCommand request, CancellationToken cancellationToken)
    {
        var merge = await db.PlayerMergeRequests
            .FirstOrDefaultAsync(r => r.Id == new PlayerMergeRequestId(request.RequestId), cancellationToken);

        if (merge is null)
            return Result.Failure(DomainErrors.PlayerMerge.NotFound);
        if (merge.RequesterId != new UserId(request.CurrentUserId))
            return Result.Failure(DomainErrors.PlayerMerge.NotRequester);

        // Deleted rather than marked withdrawn: a status nobody would ever read back is not a
        // record of anything, and the row's whole purpose is to be answered.
        db.PlayerMergeRequests.Remove(merge);
        return Result.Success();
    }
}
