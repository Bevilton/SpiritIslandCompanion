using Application.Abstractions;
using Application.Data;
using Application.Features.Friendships;
using Application.Features.Games;
using Application.Features.PlayerMerges;
using Application.Features.Statistics;
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
/// The latest merge request sent about this local player, while it is still waiting or came
/// back declined (see <paramref name="MergeDeclined"/>) — so the card can offer to withdraw it,
/// or to clear the refusal, rather than to ask again. An approved request is gone with the
/// player it was about, so there is never one to show here.
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
    bool MergeDeclined) : IWinRateTally
{
    /// <summary>
    /// The shared record shape's name for <see cref="GamesTogether"/>. Kept as an explicit member
    /// so the summary's own property can go on saying what it counts — the games the two of you
    /// sat in together, never this person's whole history — while a card that reads any tally
    /// can read this one too.
    /// </summary>
    int IPlayTally.GamesPlayed => GamesTogether;
}

internal sealed class GetPeopleHandler(IAppDbContext db, MergeRequestInbox inbox) : IQueryHandler<GetPeopleQuery, PeopleResponse>
{
    public async Task<Result<PeopleResponse>> Handle(GetPeopleQuery request, CancellationToken cancellationToken)
    {
        var userId = new UserId(request.UserId);

        var games = await db.Games
            .InvolvingUser(userId)
            .ToListAsync(cancellationToken);

        var friendships = await db.Friendships
            .AsNoTracking()
            .Involving(userId)
            .ToListAsync(cancellationToken);

        var localPlayers = await db.Players
            .AsNoTracking()
            .Where(p => p.CreatedBy == userId)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        // Pending or declined: an approved request is removed with the guest it was about
        // (see ApproveMergeRequest), so what is stored is what is still on somebody's mind.
        var merges = await db.PlayerMergeRequests
            .AsNoTracking()
            .Involving(userId)
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

        string NameOf(UserId id) => userLookup.TryGetValue(id, out var u) ? u.DisplayName : SeatNames.UnknownName;

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

        // What each of our local players has out: the latest request about them, waiting or
        // declined — a card shows one or the other.
        var latestMergeByPlayer = merges
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
                    latestMerge: null,
                    latestMergeTargetName: null);
            })
            .OrderBy(f => f.Name)
            .ToList();

        var players = localPlayers
            .Select(p =>
            {
                var latestMerge = latestMergeByPlayer.GetValueOrDefault(p.Id.Value);
                return BuildSummary(
                    games,
                    p.Name.Value,
                    email: null,
                    seat => seat.PlayerId == p.Id,
                    userId: null,
                    playerId: p.Id.Value,
                    friendshipId: null,
                    latestMerge,
                    latestMergeTargetName: latestMerge is null ? null : NameOf(latestMerge.TargetUserId));
            })
            .ToList();

        var (incomingFriendRequests, outgoingFriendRequests) = PendingFriendRequests.Split(
            friendships.Where(f => f.Status == FriendshipStatus.Pending).ToList(), userId, userLookup);

        var localNames = localPlayers.ToDictionary(p => p.Id.Value, p => p.Name.Value);

        // Outgoing merges are about our own local players, so everything they need is already
        // loaded; incoming ones reach into the requester's account and are fetched by the
        // shared loader the dashboard's nudge uses, so the two inboxes can't disagree.
        var outgoing = merges
            .Where(m => m.RequesterId == userId)
            .Select(m => new MergeRequestSummary(
                m.Id.Value,
                localNames.GetValueOrDefault(m.PlayerId.Value, MergeSubjectGames.MergedPlayerName),
                NameOf(m.TargetUserId),
                m.Status,
                gamesByPlayer.GetValueOrDefault(m.PlayerId.Value),
                m.CreatedAt))
            .OrderByDescending(r => r.CreatedAt)
            .ToList();

        var incoming = await inbox.ListAsync(userId, cancellationToken);

        return new PeopleResponse(
            friends,
            players,
            incomingFriendRequests,
            outgoingFriendRequests,
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
        PlayerMergeRequest? latestMerge,
        string? latestMergeTargetName)
    {
        var theirGames = games.Where(g => g.Players.Any(seatMatches)).ToList();
        var tally = Tally.Of(theirGames.Select(g => g.Result));

        // Ties broken by id, so the card names the same favourite on every read: seats come
        // back from the database in no particular order, and first-seen would follow it.
        var favourite = theirGames
            .SelectMany(g => g.Players.Where(seatMatches))
            .GroupBy(p => p.SpiritId.Value)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key, StringComparer.Ordinal)
            .FirstOrDefault()?.Key;

        return new PersonSummary(
            userId,
            playerId,
            name,
            email,
            friendshipId,
            tally.GamesPlayed,
            tally.Wins,
            tally.Losses,
            tally.WinRate,
            // Null while nothing is finished: Max over nullable scores answers that on its own.
            theirGames.Max(g => g.Result?.Score.Value),
            favourite,
            theirGames.Count > 0 ? theirGames.Max(g => g.StartedAt) : null,
            latestMerge?.Id.Value,
            latestMergeTargetName,
            latestMerge?.Status == PlayerMergeStatus.Rejected);
    }
}
