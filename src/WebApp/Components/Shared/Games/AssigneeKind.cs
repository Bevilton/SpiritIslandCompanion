using Application.Features.Statistics;

namespace WebApp.Components.Shared.Games;

/// <summary>
/// Who or what sits in a seat, as every surface that names a player draws it — the recorded
/// game's cards, the dashboard's table, the people lists, the statistics scope picker, the seat
/// picker's trigger. <see cref="PlayerAssigneeBadge"/> turns it into the avatar, the glyph and
/// the kind label, so the three colours (you, a friend, a guest) mean the same thing wherever
/// they turn up.
/// <para>
/// <see cref="Everyone"/> is the odd one — not a person but the unscoped choice in the statistics
/// page's player selector, drawn as one of the list it heads so that "all players" reads as a
/// choice among them rather than as a heading over them. A seat is always somebody, so
/// <see cref="AssigneeKinds.ToAssigneeKind"/> never returns it.
/// </para>
/// </summary>
public enum AssigneeKind
{
    Me,
    Friend,
    Player,
    Everyone,
    Unknown,
}

public static class AssigneeKinds
{
    /// <summary>The kind for a seat the statistics report on — every page that lists players
    /// draws them from the same tally, so they should all get the same avatar.</summary>
    public static AssigneeKind ToAssigneeKind(this PlayerKind kind) => kind switch
    {
        PlayerKind.Me     => AssigneeKind.Me,
        PlayerKind.Friend => AssigneeKind.Friend,
        _                 => AssigneeKind.Player,
    };
}
