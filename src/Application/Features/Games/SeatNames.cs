using Application.Data;
using Domain.Models.Game;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Games;

/// <summary>
/// What to call the people seated in a set of games — accounts by their display name, local
/// players by theirs — keyed by the id a seat records. Loaded by <see cref="SeatNameResolver"/>.
/// </summary>
internal sealed record SeatNames(
    IReadOnlyDictionary<Guid, string> Users,
    IReadOnlyDictionary<Guid, string> Players)
{
    /// <summary>What a person is called when the row behind their id is gone.</summary>
    public const string UnknownName = "Unknown";

    /// <summary>
    /// What to call one seat and who it is to the reader: "You" for their own account, a
    /// friend by their display name, a local player by theirs, and an empty seat as such.
    /// </summary>
    public (string Name, PlayerKind Kind) Describe(Guid? userId, Guid? playerId, Guid currentUserId)
    {
        if (userId is { } uid)
        {
            return uid == currentUserId
                ? ("You", PlayerKind.Me)
                : (Users.GetValueOrDefault(uid, UnknownName), PlayerKind.Friend);
        }

        if (playerId is { } pid)
            return (Players.GetValueOrDefault(pid, UnknownName), PlayerKind.Local);

        return ("Unassigned", PlayerKind.Unassigned);
    }
}

/// <summary>
/// Loads the <see cref="SeatNames"/> behind a set of games. Every reader that lists seats needs
/// exactly this pair of lookups, so they are loaded in one place.
/// </summary>
internal sealed class SeatNameResolver(IAppDbContext db)
{
    /// <param name="includeOwners">
    /// Also name each game's owner, who need not be seated — for a reader that speaks about
    /// the owner as a person rather than only about the seats.
    /// </param>
    public async Task<SeatNames> LoadAsync(
        IReadOnlyCollection<Game> games, CancellationToken cancellationToken, bool includeOwners = false)
    {
        var seats = games.SelectMany(g => g.Players).ToList();

        // The value-object ids go into Contains as they are: EF Core's configured conversions
        // translate the list into a SQL IN clause.
        var userIds = seats.Where(s => s.UserId is not null).Select(s => s.UserId!);
        if (includeOwners)
            userIds = userIds.Concat(games.Select(g => g.OwnerId));
        var userIdList = userIds.Distinct().ToList();

        var playerIdList = seats
            .Where(s => s.PlayerId is not null)
            .Select(s => s.PlayerId!)
            .Distinct()
            .ToList();

        var users = userIdList.Count == 0
            ? []
            : await db.Users.AsNoTracking().Where(u => userIdList.Contains(u.Id)).ToListAsync(cancellationToken);

        var players = playerIdList.Count == 0
            ? []
            : await db.Players.AsNoTracking().Where(p => playerIdList.Contains(p.Id)).ToListAsync(cancellationToken);

        return new SeatNames(
            users.ToDictionary(u => u.Id.Value, u => u.DisplayName),
            players.ToDictionary(p => p.Id.Value, p => p.Name.Value));
    }
}
