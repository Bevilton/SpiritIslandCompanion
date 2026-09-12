namespace WebApp.Components.Shared.People;

/// <summary>
/// The People page as a URL — which of its three lists is open. The sibling of
/// <see cref="Stats.StatsLink"/> and <see cref="Games.Filtering.GamesLink"/>, and for the same
/// reason: the page could already be sent to a tab and could not tell you which tab you were on,
/// so a tab was reachable by link but not returnable to.
/// </summary>
public static class PeopleLink
{
    public const string Path = "/app/people";
    public const string TabKey = "tab";

    /// <summary>
    /// The two addresses the page used to be split across, kept so a bookmark lands where it did.
    /// Named beside the page's own <c>@@page</c> directives for them, which have to stay literal,
    /// so the route resolver that reads them back compares against a constant rather than a
    /// second spelling.
    /// </summary>
    public const string LegacyFriendsPath = "/app/friends";
    public const string LegacyPlayersPath = "/app/players";

    /// <summary>The list the page opens on, and the one left out of the URL as the default.</summary>
    public const string Friends = "friends";

    public const string Players = "players";
    public const string Requests = "requests";

    public static readonly IReadOnlyList<(string Key, string Label)> Tabs =
    [
        (Friends, "Friends"),
        (Players, "Local players"),
        (Requests, "Requests"),
    ];

    public static bool IsTab(string? key) => key is not null && Tabs.Any(t => t.Key == key);

    /// <summary>One of the three lists. The default tab is left out of the query, so the plain
    /// page keeps a plain URL — the same rule <see cref="Stats.StatsLink.For"/> follows.</summary>
    public static string ForTab(string tab) =>
        IsTab(tab) && tab != Friends ? $"{Path}?{TabKey}={tab}" : Path;
}
