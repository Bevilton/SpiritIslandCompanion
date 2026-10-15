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
/// account, and the local player — now standing for nobody — is removed, and the request
/// with them. Answered, it has done the one thing it was for; nothing reads it back.
/// <para>
/// Only the target can approve. The games belong to the requester and stay theirs; what
/// changes is who is recorded as having played, which is precisely what makes them show up
/// in the approver's own history from here on.
/// </para>
/// </summary>
public sealed record ApproveMergeRequestCommand(Guid RequestId, Guid UserId) : ICommand;

internal sealed class ApproveMergeRequestHandler(IAppDbContext db, MergeSubjectGames subjectGames, TimeProvider clock)
    : ICommandHandler<ApproveMergeRequestCommand>
{
    public async Task<Result> Handle(ApproveMergeRequestCommand request, CancellationToken cancellationToken)
    {
        var merge = await db.PlayerMergeRequests
            .FirstOrDefaultAsync(r => r.Id == new PlayerMergeRequestId(request.RequestId), cancellationToken);

        if (merge is null)
            return Result.Failure(DomainErrors.PlayerMerge.NotFound);

        // The request answers first — whether this user may answer it, and whether it can still
        // be answered, are its own rules — and the seats follow. A refusal further down is
        // discarded whole by the pipeline, so nothing here has to be ordered around what was
        // already touched.
        var approved = merge.Approve(new UserId(request.UserId), clock.GetUtcNow());
        if (approved.IsFailure)
            return approved;

        // Each game hands the seats over itself, and refuses if that would seat the approver
        // twice — re-checked at the moment it would take effect, since the games may have moved
        // on since the request was sent.
        var games = await subjectGames.LoadAsync(merge.PlayerId, tracked: true, cancellationToken);
        foreach (var game in games)
        {
            var handed = game.HandSeatsToUser(merge.PlayerId, merge.TargetUserId);
            if (handed.IsFailure)
                return handed;
        }

        var player = await db.Players.FirstOrDefaultAsync(p => p.Id == merge.PlayerId, cancellationToken);
        if (player is not null)
            db.Players.Remove(player);

        // Every request naming this guest goes with them, this one included: the seats have
        // moved and the guest is gone, so none of them has anything left to say — and an
        // earlier attempt that came back declined would otherwise sit in the requester's
        // outbox for good, pointing at somebody who no longer exists.
        var requests = await db.PlayerMergeRequests
            .About(merge.PlayerId)
            .ToListAsync(cancellationToken);

        db.PlayerMergeRequests.RemoveRange(requests);

        return Result.Success();
    }
}
