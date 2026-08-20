namespace WebApp.Components.Shared.Stats;

/// <summary>
/// The statistics page as a URL: which tab is open and whose games are being counted.
/// <para>
/// Both were already readable from the query and neither was written back, so a tab was a dead
/// end — you could be sent to one but you couldn't send anyone to the one you were looking at,
/// and coming back from a game landed on Overview. The page now writes itself out here, and
/// everything that links at a slice of the statistics builds its href through the same names.
/// The sibling of <see cref="Games.Filtering.GamesLink"/>, for the same reasons.
/// </para>
/// </summary>
public static class StatsLink
{
    public const string Path = "/app/statistics";

    public const string TabKey = "tab";
    public const string UserKey = "user";
    public const string PlayerKey = "player";

    /// <summary>The tab the page opens on, and the one left out of the URL as the default.</summary>
    public const string Overview = "overview";

    public const string Players = "players";
    public const string Spirits = "spirits";
    public const string Adversaries = "adversaries";
    public const string Scenarios = "scenarios";
    public const string Boards = "boards";

    /// <summary>Every tab, in the order the page shows them, with the label on each pill.</summary>
    public static readonly IReadOnlyList<(string Key, string Label)> Tabs =
    [
        (Overview, "Overview"),
        (Players, "Players"),
        (Spirits, "Spirits"),
        (Adversaries, "Adversaries"),
        (Scenarios, "Scenarios"),
        (Boards, "Boards"),
    ];

    public static bool IsTab(string? key) => key is not null && Tabs.Any(t => t.Key == key);

    /// <summary>
    /// The page in a given state. The default tab and an unscoped page are left out of the query,
    /// so the plain page keeps a plain URL and the same state always spells the same way.
    /// </summary>
    public static string For(string? tab = null, Guid? userId = null, Guid? playerId = null)
    {
        var parts = new List<string>();
        if (IsTab(tab) && tab != Overview) parts.Add($"{TabKey}={tab}");
        if (userId is { } user) parts.Add($"{UserKey}={user}");
        else if (playerId is { } local) parts.Add($"{PlayerKey}={local}");
        return parts.Count == 0 ? Path : $"{Path}?{string.Join('&', parts)}";
    }

    /// <summary>One tab of the whole-table statistics.</summary>
    public static string ForTab(string tab) => For(tab);

    /// <summary>The statistics counted over one person's games — the People page links this way.</summary>
    public static string ForPlayer(Guid? userId, Guid? playerId, string? tab = null) =>
        For(tab, userId, playerId);
}
