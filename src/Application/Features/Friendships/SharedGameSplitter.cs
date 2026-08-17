using Application.Data;
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
/// </summary>
internal sealed class SharedGameSplitter(IAppDbContext db)
{
    /// <summary>Local players created or reused during one split, keyed by owner and name.</summary>
    private readonly Dictionary<(Guid Owner, string Name), Player> _locals = new();

    public async Task SplitAsync(UserId first, UserId second, CancellationToken cancellationToken)
    {
        var a = first.Value;
        var b = second.Value;

        var shared = await db.Games
            .Where(g =>
                (g.OwnerId.Value == a && g.Players.Any(p => p.UserId != null && p.UserId.Value == b)) ||
                (g.OwnerId.Value == b && g.Players.Any(p => p.UserId != null && p.UserId.Value == a)))
            .ToListAsync(cancellationToken);

        if (shared.Count == 0) return;

        var names = await LoadSeatNamesAsync(shared, cancellationToken);
        await LoadExistingLocalsAsync([first, second], cancellationToken);

        foreach (var game in shared)
        {
            var owner = game.OwnerId;
            var other = owner == first ? second : first;

            // The copy first: it has to read the seats as they were, before the original's
            // are rewritten underneath it.
            var seats = game.Players
                .Select(seat => seat.UserId == other
                    ? GamePlayer.CreateUserPlayer(
                        new GamePlayerId(Guid.NewGuid()),
                        seat.StartingBoard with { },
                        seat.SpiritId with { },
                        seat.AspectId is null ? null : seat.AspectId with { },
                        other)
                    : GamePlayer.CreatePlayer(
                        new GamePlayerId(Guid.NewGuid()),
                        seat.StartingBoard with { },
                        seat.SpiritId with { },
                        seat.AspectId is null ? null : seat.AspectId with { },
                        LocalFor(other, NameOf(seat, names)).Id))
                .ToList();

            db.Games.Add(game.CopyForOwner(new GameId(Guid.NewGuid()), other, seats));

            // Then the original: the departing account becomes a guest of the owner's.
            var otherName = names.Users.GetValueOrDefault(other.Value, "Former friend");
            foreach (var seat in game.Players.Where(p => p.UserId == other))
                seat.ReassignToLocalPlayer(LocalFor(owner, otherName).Id);
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
        // player's name — clip it rather than fail the split over it.
        var trimmed = name.Trim();
        if (trimmed.Length > PlayerName.MaxLength) trimmed = trimmed[..PlayerName.MaxLength];
        if (trimmed.Length == 0) trimmed = "Former friend";

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
        var ownerIds = owners.Select(o => o.Value).ToList();
        var players = await db.Players
            .Where(p => ownerIds.Contains(p.CreatedBy.Value))
            .ToListAsync(cancellationToken);

        foreach (var p in players)
            _locals.TryAdd((p.CreatedBy.Value, p.Name.Value), p);
    }

    /// <summary>Display names for everyone seated in the shared games — accounts and guests alike.</summary>
    private async Task<SeatNames> LoadSeatNamesAsync(List<Game> games, CancellationToken cancellationToken)
    {
        var userIds = games.SelectMany(g => g.Players)
            .Where(p => p.UserId is not null)
            .Select(p => p.UserId!)
            .Concat(games.Select(g => g.OwnerId))
            .Distinct()
            .ToList();

        var users = await db.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToListAsync(cancellationToken);

        var playerIds = games.SelectMany(g => g.Players)
            .Where(p => p.PlayerId is not null)
            .Select(p => p.PlayerId!)
            .Distinct()
            .ToList();

        var players = await db.Players
            .AsNoTracking()
            .Where(p => playerIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        return new SeatNames(
            users.ToDictionary(u => u.Id.Value, u => u.DisplayName),
            players.ToDictionary(p => p.Id.Value, p => p.Name.Value));
    }

    private static string NameOf(GamePlayer seat, SeatNames names) =>
        seat switch
        {
            { UserId: { } u } => names.Users.GetValueOrDefault(u.Value, "Unknown player"),
            { PlayerId: { } p } => names.Players.GetValueOrDefault(p.Value, "Unknown player"),
            _ => "Unassigned",
        };

    private sealed record SeatNames(Dictionary<Guid, string> Users, Dictionary<Guid, string> Players);
}
