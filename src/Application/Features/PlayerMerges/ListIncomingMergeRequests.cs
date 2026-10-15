using Application.Abstractions;
using Domain.Models.User;
using Domain.Results;

namespace Application.Features.PlayerMerges;

/// <summary>
/// The merges waiting on this user to answer. Small on purpose: the dashboard asks for it
/// alongside friend requests, and both are nudges rather than the page that owns them.
/// </summary>
public sealed record ListIncomingMergeRequestsQuery(Guid UserId) : IQuery<List<MergeRequestSummary>>;

internal sealed class ListIncomingMergeRequestsHandler(MergeRequestInbox inbox)
    : IQueryHandler<ListIncomingMergeRequestsQuery, List<MergeRequestSummary>>
{
    public async Task<Result<List<MergeRequestSummary>>> Handle(
        ListIncomingMergeRequestsQuery request, CancellationToken cancellationToken) =>
        await inbox.ListAsync(new UserId(request.UserId), cancellationToken);
}
