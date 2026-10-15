using Application.Abstractions;
using Application.Data;
using Domain.Errors;
using Domain.Models.PlayerMerge;
using Domain.Models.User;
using Domain.Results;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.PlayerMerges;

/// <summary>Declines the merge — "that guest wasn't me". Only the target can.</summary>
public sealed record RejectMergeRequestCommand(Guid RequestId, Guid UserId) : ICommand;

internal sealed class RejectMergeRequestHandler(IAppDbContext db, TimeProvider clock) : ICommandHandler<RejectMergeRequestCommand>
{
    public async Task<Result> Handle(RejectMergeRequestCommand request, CancellationToken cancellationToken)
    {
        var merge = await db.PlayerMergeRequests
            .FirstOrDefaultAsync(r => r.Id == new PlayerMergeRequestId(request.RequestId), cancellationToken);

        if (merge is null)
            return Result.Failure(DomainErrors.PlayerMerge.NotFound);

        // Whether this user may answer, and whether there is still anything to answer, are the
        // request's own rules.
        return merge.Reject(new UserId(request.UserId), clock.GetUtcNow());
    }
}
