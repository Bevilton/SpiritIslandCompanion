using System.Text;

namespace WebApp.Components.Shared.Games.Filtering;

/// <summary>
/// The games list as a URL: the keys its filter is spelled with, the reader that turns a query
/// back into a <see cref="GamesFilter"/>, and the builders everything else links through.
/// <para>
/// It lives in one place so a slice can only ever be spelled one way. The statistics page links
/// at "England at level 3" and the games page reads that link back into the same pills a player
/// could have set by hand — which is what makes the browser's own Back the way out of a slice,
/// and what makes a filtered list something you can bookmark or send to somebody.
/// </para>
/// </summary>
public static class GamesLink
{
    public const string Path = "/app/games";

    public const string AdversaryKey = "adversary";
    public const string LevelKey = "level";
    public const string ScenarioKey = "scenario";
    public const string SpiritKey = "spirit";
    public const string SpiritModeKey = "spirits";
    public const string BoardKey = "board";
    public const string IslandKey = "island";
    public const string WhoKey = "who";
    public const string SizeKey = "size";
    public const string DifficultyKey = "difficulty";
    public const string StatusKey = "status";
    public const string SortKey = "sort";
    public const string DirectionKey = "dir";

    /// <summary>
    /// Carries the list's own query on a game's link, so the game page can offer the way back to
    /// the slice it was opened from rather than to the whole history. Only added when there is a
    /// slice to go back to, which keeps a game's URL clean in the ordinary case.
    /// </summary>
    public const string BackKey = "back";

    /// <summary>How a status tab is written; the default tab is left out entirely.</summary>
    private static string StatusValue(GameStatusTab tab) => tab switch
    {
        GameStatusTab.Victories => "won",
        GameStatusTab.Defeats => "lost",
        GameStatusTab.Drafts => "drafts",
        _ => "",
    };

    private static GameStatusTab StatusOf(string? value) => value switch
    {
        "won" => GameStatusTab.Victories,
        "lost" => GameStatusTab.Defeats,
        "drafts" => GameStatusTab.Drafts,
        _ => GameStatusTab.All,
    };

    /// <summary>Every parameter the list understands, as the page receives them from the router.</summary>
    /// <param name="Levels">Adversary escalation levels — read against <paramref name="Adversaries"/>.</param>
    public sealed record Params(
        string[]? Adversaries = null,
        int[]? Levels = null,
        string[]? Scenarios = null,
        string[]? Spirits = null,
        string? SpiritMode = null,
        string[]? Boards = null,
        string[]? Islands = null,
        string[]? Who = null,
        int[]? Sizes = null,
        string[]? Difficulties = null,
        string? Status = null,
        string? Sort = null,
        string? Direction = null);

    /// <summary>
    /// Puts the filter into the state the URL describes — the whole state, so anything the URL
    /// leaves out goes back to its default. The list writes itself back out on every change
    /// (<see cref="Query"/>), so re-reading is idempotent: the pills and the address bar can't
    /// drift apart.
    /// </summary>
    public static void Read(Params p, GamesFilter filter)
    {
        filter.Status = StatusOf(p.Status);

        Fill(filter.Adversaries, p.Adversaries);
        Fill(filter.AdversaryLevels, p.Levels);
        Fill(filter.Scenarios, p.Scenarios);
        Fill(filter.Spirits, p.Spirits);
        Fill(filter.Boards, p.Boards);
        Fill(filter.Islands, p.Islands);
        Fill(filter.Who, p.Who);
        Fill(filter.PlayerCounts, p.Sizes);
        Fill(filter.Difficulties, p.Difficulties);

        filter.SpiritMode = p.SpiritMode == "all" ? MatchMode.All : MatchMode.Any;

        // The field first: picking one starts it from the end that field is usually wanted from,
        // so a direction read before it would be overwritten by its own default.
        filter.SetSort(
            p.Sort is { Length: > 0 } sort && filter.SortOptions.Any(o => o.Key == sort)
                ? sort
                : filter.DefaultSortKey);
        if (p.Direction is { Length: > 0 } dir) filter.Descending = dir == "desc";
    }

    private static void Fill<T>(HashSet<T> set, IEnumerable<T>? values)
    {
        set.Clear();
        foreach (var value in values ?? []) set.Add(value);
    }

    /// <summary>
    /// The filter as a query string, in a fixed order so the same slice always reads the same
    /// way. The free-text search is deliberately left out: it is a way of looking through the
    /// list rather than a slice of it, and putting every keystroke in the address bar would
    /// leave a trail of half-typed words in the browser's history.
    /// </summary>
    public static string Query(GamesFilter filter)
    {
        var q = new QueryBuilder();
        q.Add(AdversaryKey, filter.Adversaries);
        q.Add(LevelKey, filter.AdversaryLevels);
        q.Add(ScenarioKey, filter.Scenarios);
        q.Add(SpiritKey, filter.Spirits);
        if (filter.Spirits.Count > 1 && filter.SpiritMode == MatchMode.All) q.Add(SpiritModeKey, "all");
        q.Add(BoardKey, filter.Boards);
        q.Add(IslandKey, filter.Islands);
        q.Add(WhoKey, filter.Who);
        q.Add(SizeKey, filter.PlayerCounts);
        q.Add(DifficultyKey, filter.Difficulties);
        if (filter.Status != GameStatusTab.All) q.Add(StatusKey, StatusValue(filter.Status));
        // Only a departure from the list's own order is worth carrying.
        if (filter.SortChanged)
        {
            q.Add(SortKey, filter.SortKey);
            q.Add(DirectionKey, filter.Descending ? "desc" : "asc");
        }
        return q.ToString();
    }

    /// <summary>The list showing exactly what <paramref name="filter"/> shows now.</summary>
    public static string For(GamesFilter filter) => WithQuery(Query(filter));

    /// <summary>A game, with the way back to the slice it was opened from when there is one.</summary>
    public static string Game(Guid id, string? backQuery) =>
        string.IsNullOrEmpty(backQuery)
            ? $"{Path}/{id}"
            : $"{Path}/{id}?{BackKey}={Uri.EscapeDataString(backQuery)}";

    /// <summary>Where a game's "back" link goes — the slice it came from, or the whole history.</summary>
    public static string Back(string? backQuery) =>
        WithQuery(string.IsNullOrWhiteSpace(backQuery) ? null : backQuery);

    // ---- the slices the rest of the app links at ----

    /// <summary>
    /// One person's <see cref="GamesFilter.WhoKey(Guid?, Guid?)"/>, carried by every slice below.
    /// A link out of somewhere already narrowed to one person has to stay narrowed: the
    /// statistics page under its player selector counts twelve games and must not open a list of
    /// forty, and neither must a record sheet that has just said whose seats it counted.
    /// <para>
    /// The list is game-granular, so this means "games that person sat in" rather than "the seats
    /// they sat in" — the same slice the scoped statistics themselves are built from.
    /// </para>
    /// </summary>
    private static void AddWho(QueryBuilder q, string? who)
    {
        if (!string.IsNullOrEmpty(who)) q.Add(WhoKey, who);
    }

    /// <param name="level">One escalation level, or null for every game against this foe.</param>
    /// <param name="status">Narrows to won / lost / unfinished — the loss leaderboards use it.</param>
    /// <param name="who">The person the link's origin was scoped to — see <see cref="AddWho"/>.</param>
    public static string ForAdversary(
        string adversaryId, int? level = null, GameStatusTab? status = null, string? who = null)
    {
        var q = new QueryBuilder();
        q.Add(AdversaryKey, adversaryId);
        if (level is { } l) q.Add(LevelKey, l.ToString());
        AddWho(q, who);
        AddStatus(q, status);
        return WithQuery(q.ToString());
    }

    public static string ForSpirit(string spiritId, GameStatusTab? status = null, string? who = null) =>
        One(SpiritKey, spiritId, status, who);

    public static string ForScenario(string scenarioId, GameStatusTab? status = null, string? who = null) =>
        One(ScenarioKey, scenarioId, status, who);

    public static string ForBoard(string boardId, GameStatusTab? status = null, string? who = null) =>
        One(BoardKey, boardId, status, who);

    /// <summary>A published island layout, by its setup id.</summary>
    public static string ForIsland(string setupId, string? who = null) =>
        One(IslandKey, setupId, null, who);

    /// <summary>A shape from the player's own library.</summary>
    public static string ForSavedLayout(Guid layoutId, string? who = null) =>
        One(IslandKey, GamesFilter.SavedIslandKey(layoutId), null, who);

    /// <summary>Every hand-built island that was never saved, as one slice.</summary>
    public static string ForOneOffIslands(string? who = null) =>
        One(IslandKey, GamesFilter.OneOffIsland, null, who);

    /// <summary>Everything one person sat in, whichever way they are identified.</summary>
    public static string ForPlayer(Guid? userId, Guid? playerId) =>
        One(WhoKey, GamesFilter.WhoKey(userId, playerId), null, null);

    private static string One(string key, string value, GameStatusTab? status, string? who)
    {
        var q = new QueryBuilder();
        q.Add(key, value);
        AddWho(q, who);
        AddStatus(q, status);
        return WithQuery(q.ToString());
    }

    private static void AddStatus(QueryBuilder q, GameStatusTab? status)
    {
        if (status is { } tab && tab != GameStatusTab.All) q.Add(StatusKey, StatusValue(tab));
    }

    private static string WithQuery(string? query) =>
        string.IsNullOrEmpty(query) ? Path : $"{Path}?{query}";

    /// <summary>Repeated keys rather than a delimiter, which is what the router reads into arrays.</summary>
    private sealed class QueryBuilder
    {
        private readonly StringBuilder _sb = new();

        public void Add(string key, string value)
        {
            if (_sb.Length > 0) _sb.Append('&');
            _sb.Append(key).Append('=').Append(Uri.EscapeDataString(value));
        }

        public void Add(string key, IEnumerable<string> values)
        {
            // Ordered, so the same set of pills always produces the same URL — a hash set's
            // own order is not a promise, and an unstable URL would churn the address bar.
            foreach (var value in values.OrderBy(v => v, StringComparer.Ordinal)) Add(key, value);
        }

        public void Add(string key, IEnumerable<int> values)
        {
            foreach (var value in values.OrderBy(v => v)) Add(key, value.ToString());
        }

        public override string ToString() => _sb.ToString();
    }
}
