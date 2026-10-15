using Application.Abstractions;
using Application.Data;
using Domain.Errors;
using Domain.Models.PlayerMerge;
using Domain.Models.User;
using Domain.Results;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.PlayerMerges;

/// <summary>
/// Clears the requester's own request off the board — taking back one still waiting, or
/// acknowledging one that came back declined. Both are the same act from where the
/// requester sits, and neither leaves anything worth keeping. (An approved request never
/// reaches here: approval rewrites the seats and takes the request with it.)
/// </summary>
public sealed record WithdrawMergeRequestCommand(Guid RequestId, Guid UserId) : ICommand;

internal sealed class WithdrawMergeRequestHandler(IAppDbContext db) : ICommandHandler<WithdrawMergeRequestCommand>
{
    public async Task<Result> Handle(WithdrawMergeRequestCommand request, CancellationToken cancellationToken)
    {
        var merge = await db.PlayerMergeRequests
            .FirstOrDefaultAsync(r => r.Id == new PlayerMergeRequestId(request.RequestId), cancellationToken);

        if (merge is null)
            return Result.Failure(DomainErrors.PlayerMerge.NotFound);
        if (merge.RequesterId != new UserId(request.UserId))
            return Result.Failure(DomainErrors.PlayerMerge.NotRequester);

        // Deleted rather than marked withdrawn: a status nobody would ever read back is not a
        // record of anything, and the row's whole purpose is to be answered.
        db.PlayerMergeRequests.Remove(merge);
        return Result.Success();
    }
}
