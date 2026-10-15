using Application.Abstractions;
using Application.Data;
using Application.Features.Games;
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
public sealed record GetMergeRequestQuery(Guid RequestId, Guid UserId) : IQuery<MergeRequestDetail>;

public sealed record MergeRequestDetail(
    Guid Id,
    string PlayerName,
    string RequesterName,
    Guid TargetUserId,
    string TargetName,
    PlayerMergeStatus Status,
    List<MergeGameSummary> Games);

internal sealed class GetMergeRequestHandler(IAppDbContext db, MergeSubjectGames subjectGames)
    : IQueryHandler<GetMergeRequestQuery, MergeRequestDetail>
{
    public async Task<Result<MergeRequestDetail>> Handle(GetMergeRequestQuery request, CancellationToken cancellationToken)
    {
        var merge = await db.PlayerMergeRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == new PlayerMergeRequestId(request.RequestId), cancellationToken);

        if (merge is null)
            return Result.Failure<MergeRequestDetail>(DomainErrors.PlayerMerge.NotFound);
        if (!merge.InvolvesUser(new UserId(request.UserId)))
            return Result.Failure<MergeRequestDetail>(DomainErrors.PlayerMerge.NotInvolved);

        var playerName = await db.Players
            .AsNoTracking()
            .Where(p => p.Id == merge.PlayerId)
            .Select(p => p.Name.Value)
            .FirstOrDefaultAsync(cancellationToken);

        var names = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == merge.RequesterId || u.Id == merge.TargetUserId)
            .ToDictionaryAsync(u => u.Id, u => u.DisplayName, cancellationToken);

        var games = await subjectGames.LoadAsync(merge.PlayerId, tracked: false, cancellationToken);

        return new MergeRequestDetail(
            merge.Id.Value,
            playerName ?? MergeSubjectGames.MergedPlayerName,
            names.GetValueOrDefault(merge.RequesterId, SeatNames.UnknownName),
            merge.TargetUserId.Value,
            names.GetValueOrDefault(merge.TargetUserId, SeatNames.UnknownName),
            merge.Status,
            MergeSubjectGames.Summarise(games, merge.PlayerId));
    }
}
