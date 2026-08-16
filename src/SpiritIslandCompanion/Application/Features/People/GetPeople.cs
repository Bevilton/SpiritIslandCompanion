using Application.Abstractions;
using Application.Data;
using Application.Features.Games;
using Application.Features.PlayerMerges;
using Domain.Models.Friendship;
using Domain.Models.Game;
using Domain.Models.PlayerMerge;
using Domain.Models.User;
using Domain.Results;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.People;

/// <summary>
/// Everything the People page shows, in one round trip: who you play with, what your record
/// with each of them is, and what is waiting on somebody's answer.
/// <para>
/// Every tally here is computed over <see cref="GameQueries.InvolvingUser"/> — the games the
/// asking user is part of. A friend's card therefore reports your shared table, never their
/// own history: what they got up to at other people's tables is theirs to show, not ours.
/// </para>
/// </summary>
public sealed record GetPeopleQuery(Guid UserId) : IQuery<PeopleResponse>;

public sealed record PeopleResponse(
    List<PersonSummary> Friends,
    List<PersonSummary> LocalPlayers,
    List<PendingFriendRequest> IncomingFriendRequests,
    List<PendingFriendRequest> OutgoingFriendRequests,
    List<MergeRequestSummary> IncomingMergeRequests,
    List<MergeRequestSummary> OutgoingMergeRequests);

/// <param name="UserId">Set for you and for friends; null for a local player.</param>
/// <param name="PlayerId">Set for a local player; null for an account.</param>
/// <param name="FriendshipId">The friendship to remove — set only on friends.</param>
/// <param name="PendingMergeRequestId">
/// A merge already out for this local player, so the card can offer to withdraw it rather
/// than to ask again.
/// </param>
public sealed record PersonSummary(
    Guid? UserId,
    Guid? PlayerId,
    string Name,
    string? Email,
    Guid? FriendshipId,
    int GamesTogether,
    int Wins,
    int Losses,
    double WinRate,
    int? BestScore,
    string? FavouriteSpiritId,
    DateTimeOffset? LastPlayedAt,
    Guid? PendingMergeRequestId,
    string? PendingMergeTargetName,
    bool MergeDeclined);

public sealed record PendingFriendRequest(
    Guid FriendshipId,
    Guid UserId,
    string Name,
    string Email,
    DateTimeOffset SentAt);

internal sealed class GetPeopleHandler(IAppDbContext db) : IQueryHandler<GetPeopleQuery, PeopleResponse>
{
    public async Task<Result<PeopleResponse>> Handle(GetPeopleQuery request, CancellationToken cancellationToken)
    {
        var userId = new UserId(request.UserId);

        var games = await db.Games
            .InvolvingUser(request.UserId)
            .ToListAsync(cancellationToken);

        var friendships = await db.Friendships
            .AsNoTracking()
            .Where(f => f.RequesterId.Value == request.UserId || f.AddresseeId.Value == request.UserId)
            .ToListAsync(cancellationToken);

        var localPlayers = await db.Players
            .AsNoTracking()
            .Where(p => p.CreatedBy.Value == request.UserId)
            .OrderBy(p => p.Name.Value)
            .ToListAsync(cancellationToken);

        var merges = await db.PlayerMergeRequests
            .AsNoTracking()
            .Where(r => (r.RequesterId.Value == request.UserId || r.TargetUserId.Value == request.UserId) &&
                        r.Status != PlayerMergeStatus.Approved)
            .ToListAsync(cancellationToken);

        // Everyone named anywhere on this page: the two sides of every friendship, both ends
        // of every merge request, and me.
        var referencedUserIds = friendships
            .SelectMany(f => new[] { f.RequesterId, f.AddresseeId })
            .Concat(merges.SelectMany(m => new[] { m.RequesterId, m.TargetUserId }))
            .Append(userId)
            .Distinct()
            .ToList();

        var users = await db.Users
            .AsNoTracking()
            .Where(u => referencedUserIds.Contains(u.Id))
            .ToListAsync(cancellationToken);

        var userLookup = users.ToDictionary(u => u.Id);

        string NameOf(UserId id) => userLookup.TryGetValue(id, out var u) ? u.DisplayName : "Unknown";

        // Games per local player, for the merge-request counts and the seat tallies. Local
        // players only ever sit in their owner's games, which are all in `games`.
        var gamesByPlayer = games
            .SelectMany(g => g.Players
                .Where(p => p.PlayerId is not null)
                .Select(p => p.PlayerId!.Value)
                .Distinct()
                .Select(pid => (PlayerId: pid, Game: g)))
            .GroupBy(x => x.PlayerId)
            .ToDictionary(g => g.Key, g => g.Count());

        var pendingByPlayer = merges
            .Where(m => m.RequesterId == userId)
            .GroupBy(m => m.PlayerId.Value)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(m => m.CreatedAt).First());

        var friends = friendships
            .Where(f => f.Status == FriendshipStatus.Accepted)
            .Select(f =>
            {
                var otherId = f.GetOtherUserId(userId);
                return BuildSummary(
                    games,
                    NameOf(otherId),
                    userLookup.TryGetValue(otherId, out var u) ? u.Email.Value : null,
                    p => p.UserId == otherId,
                    otherId.Value,
                    playerId: null,
                    friendshipId: f.Id.Value,
                    pending: null,
                    pendingTargetName: null);
            })
            .OrderBy(f => f.Name)
            .ToList();

        var players = localPlayers
            .Select(p =>
            {
                var pending = pendingByPlayer.GetValueOrDefault(p.Id.Value);
                return BuildSummary(
                    games,
                    p.Name.Value,
                    email: null,
                    seat => seat.PlayerId == p.Id,
                    userId: null,
                    playerId: p.Id.Value,
                    friendshipId: null,
                    pending: pending,
                    pendingTargetName: pending is null ? null : NameOf(pending.TargetUserId));
            })
            .ToList();

        var pendingFriendships = friendships.Where(f => f.Status == FriendshipStatus.Pending).ToList();

        PendingFriendRequest ToFriendRequest(Friendship f, UserId otherId) =>
            new(f.Id.Value,
                otherId.Value,
                NameOf(otherId),
                userLookup.TryGetValue(otherId, out var u) ? u.Email.Value : "",
                f.CreatedAt);

        var localNames = localPlayers.ToDictionary(p => p.Id.Value, p => p.Name.Value);

        // Outgoing merges are about our own local players, so everything they need is already
        // loaded; incoming ones reach into the requester's account and are fetched by the
        // shared loader the dashboard's nudge uses, so the two inboxes can't disagree.
        var outgoing = merges
            .Where(m => m.RequesterId == userId)
            .Select(m => new MergeRequestSummary(
                m.Id.Value,
                localNames.GetValueOrDefault(m.PlayerId.Value, "Merged player"),
                NameOf(m.TargetUserId),
                m.Status,
                gamesByPlayer.GetValueOrDefault(m.PlayerId.Value),
                m.CreatedAt))
            .OrderByDescending(r => r.CreatedAt)
            .ToList();

        var incoming = await MergeRequestSummaries.IncomingAsync(db, request.UserId, cancellationToken);

        return new PeopleResponse(
            friends,
            players,
            pendingFriendships.Where(f => f.AddresseeId == userId)
                .Select(f => ToFriendRequest(f, f.RequesterId))
                .OrderByDescending(r => r.SentAt).ToList(),
            pendingFriendships.Where(f => f.RequesterId == userId)
                .Select(f => ToFriendRequest(f, f.AddresseeId))
                .OrderByDescending(r => r.SentAt).ToList(),
            incoming,
            outgoing);
    }

    /// <summary>One person's record across the games they and the asking user both sat in.</summary>
    private static PersonSummary BuildSummary(
        List<Game> games,
        string name,
        string? email,
        Func<GamePlayer, bool> seatMatches,
        Guid? userId,
        Guid? playerId,
        Guid? friendshipId,
        PlayerMergeRequest? pending,
        string? pendingTargetName)
    {
        var theirGames = games.Where(g => g.Players.Any(seatMatches)).ToList();
        var completed = theirGames.Where(g => g.Result is not null).ToList();
        var wins = completed.Count(g => g.Result!.Win);

        var favourite = theirGames
            .SelectMany(g => g.Players.Where(seatMatches))
            .GroupBy(p => p.SpiritId.Value)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault()?.Key;

        return new PersonSummary(
            userId,
            playerId,
            name,
            email,
            friendshipId,
            theirGames.Count,
            wins,
            completed.Count - wins,
            completed.Count > 0 ? (double)wins / completed.Count * 100 : 0,
            completed.Count > 0 ? completed.Max(g => g.Result!.Score.Value) : null,
            favourite,
            theirGames.Count > 0 ? theirGames.Max(g => g.StartedAt) : null,
            pending?.Id.Value,
            pendingTargetName,
            pending?.Status == PlayerMergeStatus.Rejected);
    }
}
