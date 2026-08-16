using Application.Abstractions;
using Application.Data;
using Domain.Errors;
using Domain.Models.PlayerMerge;
using Domain.Models.User;
using Domain.Results;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.PlayerMerges;

/// <summary>
/// One merge request with the games it would move, for either side to read before
/// answering. The games are the whole point: nobody should agree to take over seats they
/// haven't seen.
/// </summary>
public sealed record GetMergeRequestQuery(Guid RequestId, Guid CurrentUserId) : IQuery<MergeRequestDetail>;

public sealed record MergeRequestDetail(
    Guid Id,
    string PlayerName,
    string RequesterName,
    Guid TargetUserId,
    string TargetName,
    PlayerMergeStatus Status,
    List<MergeGameSummary> Games);

internal sealed class GetMergeRequestHandler(IAppDbContext db) : IQueryHandler<GetMergeRequestQuery, MergeRequestDetail>
{
    public async Task<Result<MergeRequestDetail>> Handle(GetMergeRequestQuery request, CancellationToken cancellationToken)
    {
        var merge = await db.PlayerMergeRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == new PlayerMergeRequestId(request.RequestId), cancellationToken);

        if (merge is null)
            return Result.Failure<MergeRequestDetail>(DomainErrors.PlayerMerge.NotFound);
        if (!merge.InvolvesUser(new UserId(request.CurrentUserId)))
            return Result.Failure<MergeRequestDetail>(DomainErrors.PlayerMerge.NotInvolved);

        var playerName = await db.Players
            .AsNoTracking()
            .Where(p => p.Id == merge.PlayerId)
            .Select(p => p.Name.Value)
            .FirstOrDefaultAsync(cancellationToken);

        var users = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == merge.RequesterId || u.Id == merge.TargetUserId)
            .ToListAsync(cancellationToken);

        var names = users.ToDictionary(u => u.Id, u => u.DisplayName);

        var games = await MergeSubjectGames.LoadAsync(db, merge.PlayerId, tracked: false, cancellationToken);

        return new MergeRequestDetail(
            merge.Id.Value,
            // The local player is gone once the merge lands — the request still has to read
            // as a sentence about someone afterwards.
            playerName ?? "Merged player",
            names.GetValueOrDefault(merge.RequesterId, "Unknown"),
            merge.TargetUserId.Value,
            names.GetValueOrDefault(merge.TargetUserId, "Unknown"),
            merge.Status,
            MergeSubjectGames.Summarise(games, merge.PlayerId));
    }
}
