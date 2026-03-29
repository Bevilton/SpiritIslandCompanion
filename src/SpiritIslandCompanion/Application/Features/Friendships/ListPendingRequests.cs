using Application.Abstractions;
using Application.Data;
using Domain.Models.Friendship;
using Domain.Models.User;
using Domain.Results;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Friendships;

public sealed record ListPendingRequestsQuery(Guid UserId) : IQuery<PendingRequestsResponse>;

public sealed record PendingRequestsResponse(
    List<PendingRequestDto> Incoming,
    List<PendingRequestDto> Outgoing);

public sealed record PendingRequestDto(
    Guid FriendshipId,
    Guid UserId,
    string Email,
    string Nickname,
    DateTimeOffset SentAt);

internal sealed class ListPendingRequestsHandler(IAppDbContext db) : IQueryHandler<ListPendingRequestsQuery, PendingRequestsResponse>
{
    public async Task<Result<PendingRequestsResponse>> Handle(ListPendingRequestsQuery request, CancellationToken cancellationToken)
    {
        var userId = new UserId(request.UserId);

        var pendingFriendships = await db.Friendships
            .AsNoTracking()
            .Where(f => f.Status == FriendshipStatus.Pending &&
                        (f.RequesterId == userId || f.AddresseeId == userId))
            .ToListAsync(cancellationToken);

        var allUserIds = pendingFriendships
            .SelectMany(f => new[] { f.RequesterId, f.AddresseeId })
            .Where(id => id != userId)
            .Distinct()
            .ToList();

        var users = await db.Users
            .AsNoTracking()
            .Where(u => allUserIds.Contains(u.Id))
            .ToListAsync(cancellationToken);

        var userLookup = users.ToDictionary(u => u.Id);

        PendingRequestDto MapToDto(Friendship f, UserId otherUserId)
        {
            var user = userLookup.GetValueOrDefault(otherUserId);
            return new PendingRequestDto(
                f.Id.Value,
                otherUserId.Value,
                user?.Email.Value ?? "",
                user?.Nickname.Value ?? "",
                f.CreatedAt);
        }

        var incoming = pendingFriendships
            .Where(f => f.AddresseeId == userId)
            .Select(f => MapToDto(f, f.RequesterId))
            .OrderByDescending(r => r.SentAt)
            .ToList();

        var outgoing = pendingFriendships
            .Where(f => f.RequesterId == userId)
            .Select(f => MapToDto(f, f.AddresseeId))
            .OrderByDescending(r => r.SentAt)
            .ToList();

        return new PendingRequestsResponse(incoming, outgoing);
    }
}
