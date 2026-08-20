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

    public static string ForTab(string? tab) =>
        IsTab(tab) && tab != Friends ? $"{Path}?{TabKey}={tab}" : Path;
}
