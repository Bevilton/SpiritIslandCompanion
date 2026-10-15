using Application.Data;
using Application.Features.Games;
using Domain.Models.Game;
using Domain.Models.Player;
using Domain.Models.User;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Friendships;

/// <summary>
/// Unpicks two accounts from the games they played together.
/// <para>
/// A shared game is one account's record with the other sitting in it, and only the owner can
/// see it once the seat is gone — so ending the friendship by simply cutting the link would
/// take the evening away from whoever did not write it down. Instead each side comes out of it
/// owning the games they were part of: the owner keeps the original with the other person
/// turned into a local player, and the other person is handed a copy of their own.
/// </para>
/// <para>
/// In that copy every seat except the new owner's becomes a local player, including seats held
/// by third parties. Carrying an account reference across would hand someone a game with a
/// stranger in it — and, worse, put that stranger's history in a list they never agreed to
/// appear in. A name is all the copy needs, and a name is all it keeps.
/// </para>
/// <para>
/// One instance per split: it remembers the local players it created along the way, so a
/// person seated in several of the shared games becomes one local player, not one per game —
/// which is why it is registered transient rather than scoped.
/// </para>
/// </summary>
internal sealed class SharedGameSplitter(IAppDbContext db, SeatNameResolver seatNames)
{
    private const string FormerFriendName = "Former friend";
    private const string UnknownPlayerName = "Unknown player";

    /// <summary>Local players created or reused during one split, keyed by owner and name.</summary>
    private readonly Dictionary<(Guid Owner, string Name), Player> _locals = new();

    public async Task SplitAsync(UserId first, UserId second, CancellationToken cancellationToken)
    {
        var shared = await db.Games
            .Where(g =>
                (g.OwnerId == first && g.Players.Any(p => p.UserId == second)) ||
                (g.OwnerId == second && g.Players.Any(p => p.UserId == first)))
            .ToListAsync(cancellationToken);

        if (shared.Count == 0) return;

        var names = await seatNames.LoadAsync(shared, cancellationToken, includeOwners: true);
        await LoadExistingLocalsAsync([first, second], cancellationToken);

        foreach (var game in shared)
        {
            var owner = game.OwnerId;
            var other = owner == first ? second : first;

            // The copy first: it has to read the seats as they were, before the original's
            // are rewritten underneath it. Which seat stays the new owner's and which become
            // their guests is the game's own rule; only the guests' names are ours to supply.
            db.Games.Add(game.CopyForOwner(
                new GameId(Guid.NewGuid()), other, seat => LocalFor(other, NameOf(seat, names)).Id));

            // Then the original: the departing account becomes a guest of the owner's.
            var otherName = names.Users.GetValueOrDefault(other.Value, FormerFriendName);
            game.HandSeatsToLocalPlayer(other, LocalFor(owner, otherName).Id);
        }
    }

    /// <summary>
    /// A local player of <paramref name="owner"/> called <paramref name="name"/>, reusing one
    /// they already have. Same name in the same address book is the same person — creating a
    /// second "Anna" beside the first would split her record in two for no reason.
    /// </summary>
    private Player LocalFor(UserId owner, string name)
    {
        // A display name can be an email address, which is allowed to be longer than a local
        // player's name — clip it rather than fail the split over it. Trimmed after clipping
        // too, so the key below is the name exactly as PlayerName will store it.
        var trimmed = name.Trim();
        if (trimmed.Length > PlayerName.MaxLength) trimmed = trimmed[..PlayerName.MaxLength].TrimEnd();
        if (trimmed.Length == 0) trimmed = FormerFriendName;

        var key = (owner.Value, trimmed);
        if (_locals.TryGetValue(key, out var existing)) return existing;

        var created = Player.Create(
            new PlayerId(Guid.NewGuid()),
            PlayerName.Create(trimmed).Value,
            owner);

        db.Players.Add(created);
        _locals[key] = created;
        return created;
    }

    private async Task LoadExistingLocalsAsync(UserId[] owners, CancellationToken cancellationToken)
    {
        // Read only: an existing local player is reused by id, never changed by the split.
        var players = await db.Players
            .AsNoTracking()
            .Where(p => owners.Contains(p.CreatedBy))
            .ToListAsync(cancellationToken);

        foreach (var p in players)
            _locals.TryAdd((p.CreatedBy.Value, p.Name.Value), p);
    }

    /// <summary>Who holds an assigned seat — an account by its display name, a local player by theirs.</summary>
    private static string NameOf(GamePlayer seat, SeatNames names) =>
        seat.UserId is { } user
            ? names.Users.GetValueOrDefault(user.Value, UnknownPlayerName)
            : names.Players.GetValueOrDefault(seat.PlayerId!.Value, UnknownPlayerName);
}
