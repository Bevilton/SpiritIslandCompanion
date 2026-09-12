using Application.Features.Friendships;
using Application.Features.Players;

namespace WebApp.Components.Shared.Games;

/// <summary>
/// How a seat's occupant is spelled while a game is being set up: <c>me</c>, <c>friend:{userId}</c>
/// or <c>player:{playerId}</c>. One string rather than a discriminated pair because the picker
/// that edits it is a plain single-select control, and a select's value is a string.
/// <para>
/// The format lived nowhere and was known everywhere: the picker built the three shapes and the
/// setup page took them apart again, each with its own <c>StartsWith("friend:")</c> and its own
/// <c>value["friend:".Length..]</c> — a dozen literals across two files, any one of which could
/// have been misspelled without the compiler noticing. Building and reading it both happen here
/// now, so the two halves cannot disagree.
/// </para>
/// </summary>
public static class SeatAssignee
{
    /// <summary>The signed-in account's own seat, and what a new seat starts as.</summary>
    public const string Me = "me";

    private const string FriendPrefix = "friend:";
    private const string PlayerPrefix = "player:";

    public static string ForFriend(Guid userId) => $"{FriendPrefix}{userId}";
    public static string ForPlayer(Guid playerId) => $"{PlayerPrefix}{playerId}";

    public static bool IsMe(string value) => value == Me;

    /// <summary>The friend's account id, or null when the seat is not a friend's.</summary>
    public static Guid? FriendId(string value) => IdAfter(value, FriendPrefix);

    /// <summary>The local player's id, or null when the seat is not a guest's.</summary>
    public static Guid? PlayerId(string value) => IdAfter(value, PlayerPrefix);

    /// <summary>
    /// Who the value names, looked up in the table's own lists. The name is null for the reader's
    /// own seat, which every caller words for itself ("Me" on the picker, "You" on the legend),
    /// and for a friend or a guest the lists no longer hold — a seat kept after the person was
    /// removed. The two controls that read a seat's occupant back had each parsed the value and
    /// walked the lists themselves, and had settled the fallbacks two different ways.
    /// </summary>
    public static (AssigneeKind Kind, string? Name) Resolve(
        string value, IReadOnlyList<FriendResponse> friends, IReadOnlyList<ListPlayersResponse> players)
    {
        if (IsMe(value)) return (AssigneeKind.Me, null);

        if (FriendId(value) is { } friendId)
            return (AssigneeKind.Friend, friends.FirstOrDefault(f => f.FriendUserId == friendId)?.Name);

        if (PlayerId(value) is { } playerId)
            return (AssigneeKind.Player, players.FirstOrDefault(p => p.Id == playerId)?.Name);

        return (AssigneeKind.Unknown, null);
    }

    /// <summary>
    /// Who the value names, as the setup screen writes it beside a seat: "You" for the reader's
    /// own, the person's name, or the kind's word when the lists no longer hold them. The picker
    /// words the same cases for itself ("Me", "Unknown friend") because it is a control rather
    /// than a caption; the setup screen's legend, scope notes and board chips all say it this way.
    /// </summary>
    public static string Label(
        string value, IReadOnlyList<FriendResponse> friends, IReadOnlyList<ListPlayersResponse> players) =>
        Resolve(value, friends, players) switch
        {
            (AssigneeKind.Me, _)            => "You",
            (AssigneeKind.Friend, var name) => name ?? "Friend",
            (AssigneeKind.Player, var name) => name ?? "Player",
            _                               => "Unassigned",
        };

    /// <summary>
    /// The initials a board on the island wears for whoever sits at it — null for the reader's
    /// own seat, which every seat starts as: a "Y" for "You" on all four boards labels nothing,
    /// and once someone else is at the table the unlabelled board is the one that is yours.
    /// </summary>
    public static string? Initials(
        string value, IReadOnlyList<FriendResponse> friends, IReadOnlyList<ListPlayersResponse> players)
    {
        if (IsMe(value)) return null;
        var parts = Label(value, friends, players)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length switch
        {
            0 => "?",
            1 => parts[0][..1],
            _ => $"{parts[0][..1]}{parts[1][..1]}",
        };
    }

    private static Guid? IdAfter(string value, string prefix) =>
        value.StartsWith(prefix, StringComparison.Ordinal)
        && Guid.TryParse(value[prefix.Length..], out var id)
            ? id
            : null;
}
