using Application.Features.Games;
using Domain.Models.Static.Data;

namespace WebApp.Components.Shared.Games.Filtering;

/// <summary>
/// The always-visible slice above the games filter bar. A tab, not a pill in the bar: whether
/// you're looking at victories, defeats or unfinished drafts is the first question of the page,
/// so it shouldn't hide behind a disclosure — and it survives the bar's "Clear".
/// </summary>
public enum GameStatusTab { All, Victories, Defeats, Drafts }

/// <summary>
/// The games-list filter: the status tab, the facets in the bar (adversary, scenario, spirit,
/// players, difficulty), the free-text search and the ordering. Built on the catalogue
/// <see cref="FilterState"/> so <c>FilterBar</c> renders it with the same shell every browse
/// page and picker already has — games just have no expansion or play-history rows.
/// </summary>
public sealed class GamesFilter : FilterState
{
    public GamesFilter() : base(Sorts, ByDate.Key) { }

    public static readonly SortOption ByDate = new("date", "Date", "Oldest", "Newest", StartsDescending: true);
    public static readonly SortOption ByScore = new("score", "Score", "Lowest", "Highest", StartsDescending: true);
    public static readonly SortOption ByPlayers = new("players", "Players", "Fewest", "Most");

    private static readonly IReadOnlyList<SortOption> Sorts = [ByDate, ByDifficulty, ByScore, ByPlayers];

    /// <summary>Games belong to nobody's expansion — the row would be a control with no answer.</summary>
    public override bool HasExpansions => false;

    /// <summary>Facet key meaning "games without one" — an id no catalogue entry uses.</summary>
    public const string None = "__none";

    public GameStatusTab Status { get; set; } = GameStatusTab.All;

    public HashSet<string> Adversaries { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Escalation levels to keep. Read against the picked foes when there are any: "England"
    /// and "3" together has to mean England was at level 3, not that some other foe on the
    /// table happened to be — otherwise the pair would answer a question nobody asked.
    /// </summary>
    public HashSet<int> AdversaryLevels { get; } = [];

    public HashSet<string> Scenarios { get; } = new(StringComparer.Ordinal);
    public HashSet<string> Spirits { get; } = new(StringComparer.Ordinal);

    /// <summary>Whether picked spirits mean "all at the same table" or "any of them played".</summary>
    public MatchMode SpiritMode { get; set; } = MatchMode.Any;

    /// <summary>Starting boards to keep — any seat on one of them is enough.</summary>
    public HashSet<string> Boards { get; } = new(StringComparer.Ordinal);

    /// <summary>Islands to keep, keyed by <see cref="IslandKeyOf"/>.</summary>
    public HashSet<string> Islands { get; } = new(StringComparer.Ordinal);

    /// <summary>People at the table, keyed by <see cref="WhoKeyOf"/>.</summary>
    public HashSet<string> Who { get; } = new(StringComparer.Ordinal);

    public HashSet<int> PlayerCounts { get; } = [];
    public HashSet<string> Difficulties { get; } = new(StringComparer.Ordinal);

    /// <summary>The one-off hand-built islands, which have no shape of their own to name.</summary>
    public const string OneOffIsland = "one-off";

    /// <summary>A shape from the player's library, as an island facet key.</summary>
    public static string SavedIslandKey(Guid layoutId) => $"saved:{layoutId}";

    /// <summary>
    /// Which island a game was played on, as one facet key: a published layout by its setup id,
    /// a shape from the library by its own id, and everything else — a one-off arrangement, or
    /// one whose saved shape has been deleted since — pooled as <see cref="OneOffIsland"/>. The
    /// same grouping the statistics island tiles use, so a tile and a pill always agree.
    /// </summary>
    public static string IslandKeyOf(ListGamesResponse game) =>
        !IslandSetups.IsCustomId(game.IslandSetupId) ? game.IslandSetupId
        : game.CustomLayoutId is { } saved && game.CustomLayoutName is not null ? SavedIslandKey(saved)
        : OneOffIsland;

    /// <summary>A seat with nobody named in it — the facet key for "unassigned".</summary>
    public const string UnknownPlayer = "unassigned";

    public static string WhoKeyOf(GamePlayerSummary player) => WhoKey(player.UserId, player.PlayerId);

    /// <summary>One person, however they are identified: an account, a local player, or neither.</summary>
    public static string WhoKey(Guid? userId, Guid? playerId) =>
        userId is { } user ? $"u:{user}"
        : playerId is { } local ? $"p:{local}"
        : UnknownPlayer;

    /// <summary>
    /// One difficulty band of the list. Bands rather than an exact number: nobody remembers a
    /// game as "difficulty 7", they remember it as roughly gentle or roughly brutal.
    /// </summary>
    public sealed record DifficultyBand(string Key, string Label, int Min, int Max)
    {
        public bool Contains(int difficulty) => difficulty >= Min && difficulty <= Max;
    }

    public static readonly IReadOnlyList<DifficultyBand> AllDifficultyBands =
    [
        new("0-3", "0–3", 0, 3),
        new("4-7", "4–7", 4, 7),
        new("8-10", "8–10", 8, 10),
        new("11+", "11+", 11, int.MaxValue),
    ];

    public override int ActiveCount =>
        Adversaries.Count + AdversaryLevels.Count + Scenarios.Count + Spirits.Count
        + Boards.Count + Islands.Count + Who.Count + PlayerCounts.Count + Difficulties.Count;

    /// <summary>A full reset returns the tab to All too — unlike the bar's Clear below.</summary>
    protected override void ResetExtras() => Status = GameStatusTab.All;

    /// <summary>The bar's Clear. The status tab is not the bar's to clear — it lives above it.</summary>
    public override void Clear()
    {
        base.Clear();
        Adversaries.Clear();
        AdversaryLevels.Clear();
        Scenarios.Clear();
        Spirits.Clear();
        Boards.Clear();
        Islands.Clear();
        Who.Clear();
        PlayerCounts.Clear();
        Difficulties.Clear();
        SpiritMode = MatchMode.Any;
    }

    /// <summary>Whether the game survives the status tab above the bar.</summary>
    public bool MatchesStatus(ListGamesResponse game) => Status switch
    {
        GameStatusTab.Victories => game is { IsCompleted: true, Win: true },
        GameStatusTab.Defeats => game is { IsCompleted: true, Win: false },
        GameStatusTab.Drafts => !game.IsCompleted,
        _ => true,
    };

    /// <summary>
    /// Everything except the status tab. Kept apart from it so the tab counts can be taken over
    /// the games the rest of the filter already allows — with a slice linked in from the
    /// statistics page, "42 victories" would otherwise be counting a history nobody can see.
    /// </summary>
    public bool MatchesFacets(ListGamesResponse game)
    {
        if (!MatchesOrNone(Adversaries, game.Adversaries.Select(a => a.AdversaryId).ToList())) return false;

        if (AdversaryLevels.Count > 0)
        {
            var relevant = Adversaries.Count > 0
                ? game.Adversaries.Where(a => Adversaries.Contains(a.AdversaryId))
                : game.Adversaries;
            if (!relevant.Any(a => AdversaryLevels.Contains(a.Level))) return false;
        }

        if (!MatchesOrNone(Scenarios, game.ScenarioId is null ? [] : [game.ScenarioId])) return false;

        if (Spirits.Count > 0)
        {
            var atTable = game.Players.Select(p => p.SpiritId).ToList();
            if (!MatchesGroup(Spirits, atTable, SpiritMode)) return false;
        }

        if (Boards.Count > 0 && !game.Players.Any(p => Boards.Contains(p.BoardId))) return false;

        if (Islands.Count > 0 && !Islands.Contains(IslandKeyOf(game))) return false;

        if (Who.Count > 0 && !game.Players.Any(p => Who.Contains(WhoKeyOf(p)))) return false;

        if (PlayerCounts.Count > 0 && !PlayerCounts.Contains(game.PlayerCount)) return false;

        if (Difficulties.Count > 0
            && !AllDifficultyBands.Any(b => Difficulties.Contains(b.Key) && b.Contains(game.Difficulty)))
        {
            return false;
        }

        return MatchesQuery(game);
    }

    public bool Matches(ListGamesResponse game) => MatchesStatus(game) && MatchesFacets(game);

    public List<ListGamesResponse> Apply(IEnumerable<ListGamesResponse> games)
    {
        var kept = games.Where(Matches);
        return SortKey switch
        {
            // Ties broken by date, newest first, so equal rows keep the reading order of the page.
            "difficulty" => By(kept, g => g.Difficulty).ThenByDescending(g => g.StartedAt).ToList(),
            "players" => By(kept, g => g.PlayerCount).ThenByDescending(g => g.StartedAt).ToList(),
            // Drafts have no score; pin them after the scored games from either direction.
            "score" => By(kept, g => g.Score ?? (Descending ? int.MinValue : int.MaxValue))
                .ThenByDescending(g => g.StartedAt).ToList(),
            _ => By(kept, g => g.StartedAt).ToList(),
        };
    }

    /// <summary>Month sections only make sense while the list is in date order.</summary>
    public bool GroupsByMonth => SortKey == ByDate.Key;

    /// <summary>One pill currently narrowing the list, named and undoable.</summary>
    /// <param name="Group">Which row it came from, for the chip's tooltip.</param>
    public sealed record ActiveFacet(string Label, string Group, Action Remove);

    /// <summary>
    /// What is currently narrowing the list, in the order the filter rows ask it. Shown above the
    /// bar rather than only counted in its header: a slice arrived at by link has to say what it
    /// is, and each chip has to be undoable one at a time — coming in on "England at level 3" and
    /// wanting "all England games" shouldn't mean starting over.
    /// </summary>
    public IReadOnlyList<ActiveFacet> ActiveFacets(IReadOnlyList<ListGamesResponse> games)
    {
        var facets = new List<ActiveFacet>();

        foreach (var id in Adversaries.OrderBy(a => a, StringComparer.Ordinal))
        {
            var label = id == None ? "No adversary" : GameLookups.AdversaryFor(id)?.Name ?? id;
            facets.Add(new ActiveFacet(label, "Adversary", () => Adversaries.Remove(id)));
        }
        foreach (var level in AdversaryLevels.OrderBy(l => l))
            facets.Add(new ActiveFacet($"Level {level}", "Adversary level", () => AdversaryLevels.Remove(level)));

        foreach (var id in Scenarios.OrderBy(s => s, StringComparer.Ordinal))
        {
            var label = id == None ? "No scenario" : GameLookups.ScenarioFor(id)?.Name ?? id;
            facets.Add(new ActiveFacet(label, "Scenario", () => Scenarios.Remove(id)));
        }

        foreach (var id in Spirits.OrderBy(s => s, StringComparer.Ordinal))
            facets.Add(new ActiveFacet(GameLookups.SpiritFor(id)?.Name ?? id, "Spirit", () => Spirits.Remove(id)));

        foreach (var id in Boards.OrderBy(b => b, StringComparer.Ordinal))
        {
            var label = GameLookups.BoardFor(id) is { } board ? $"Board {GameLookups.BoardLetter(board)}" : id;
            facets.Add(new ActiveFacet(label, "Board", () => Boards.Remove(id)));
        }

        foreach (var key in Islands.OrderBy(i => i, StringComparer.Ordinal))
            facets.Add(new ActiveFacet(IslandLabel(key, games), "Island", () => Islands.Remove(key)));

        foreach (var key in Who.OrderBy(w => w, StringComparer.Ordinal))
            facets.Add(new ActiveFacet(WhoLabel(key, games), "At the table", () => Who.Remove(key)));

        foreach (var count in PlayerCounts.OrderBy(c => c))
        {
            var label = count == 1 ? "Solo" : $"{count} players";
            facets.Add(new ActiveFacet(label, "Table size", () => PlayerCounts.Remove(count)));
        }

        foreach (var band in AllDifficultyBands.Where(b => Difficulties.Contains(b.Key)))
            facets.Add(new ActiveFacet($"Difficulty {band.Label}", "Difficulty", () => Difficulties.Remove(band.Key)));

        return facets;
    }

    /// <summary>An island facet key as words — the saved shapes are named by the games that used them.</summary>
    public static string IslandLabel(string key, IReadOnlyList<ListGamesResponse> games)
    {
        if (key == OneOffIsland) return "One-off island";
        if (key.StartsWith("saved:", StringComparison.Ordinal))
        {
            return games.FirstOrDefault(g => IslandKeyOf(g) == key)?.CustomLayoutName ?? "Saved island";
        }
        return GameLookups.IslandSetupFor(key) is { } setup
            ? $"{setup.Name} · {setup.NumberOfPlayers}"
            : key;
    }

    /// <summary>A person facet key as a name — resolved from the seats that carry it.</summary>
    public static string WhoLabel(string key, IReadOnlyList<ListGamesResponse> games) =>
        games.SelectMany(g => g.Players).FirstOrDefault(p => WhoKeyOf(p) == key)?.Name
        ?? (key == UnknownPlayer ? "Unassigned" : "Unknown");

    /// <summary>"Any of the picked, or none at all when <see cref="None"/> is picked."</summary>
    private static bool MatchesOrNone(HashSet<string> picked, IReadOnlyList<string> has)
    {
        if (picked.Count == 0) return true;
        if (has.Count == 0) return picked.Contains(None);
        return has.Any(picked.Contains);
    }

    /// <summary>The search box reaches everything a row shows a name for.</summary>
    private bool MatchesQuery(ListGamesResponse game)
    {
        if (string.IsNullOrWhiteSpace(Query)) return true;

        return game.Adversaries.Any(a => Has(GameLookups.AdversaryFor(a.AdversaryId)?.Name))
               || Has(GameLookups.ScenarioFor(game.ScenarioId)?.Name)
               || Has(game.CustomLayoutName)
               || game.Players.Any(p => Has(p.Name) || Has(GameLookups.SpiritFor(p.SpiritId)?.Name));

        bool Has(string? name) =>
            name is not null && name.Contains(Query, StringComparison.OrdinalIgnoreCase);
    }
}
