using Application.Features.Games;

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
    public HashSet<string> Scenarios { get; } = new(StringComparer.Ordinal);
    public HashSet<string> Spirits { get; } = new(StringComparer.Ordinal);

    /// <summary>Whether picked spirits mean "all at the same table" or "any of them played".</summary>
    public MatchMode SpiritMode { get; set; } = MatchMode.Any;

    public HashSet<int> PlayerCounts { get; } = [];
    public HashSet<string> Difficulties { get; } = new(StringComparer.Ordinal);

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
        Adversaries.Count + Scenarios.Count + Spirits.Count + PlayerCounts.Count + Difficulties.Count;

    /// <summary>A full reset returns the tab to All too — unlike the bar's Clear below.</summary>
    protected override void ResetExtras() => Status = GameStatusTab.All;

    /// <summary>The bar's Clear. The status tab is not the bar's to clear — it lives above it.</summary>
    public override void Clear()
    {
        base.Clear();
        Adversaries.Clear();
        Scenarios.Clear();
        Spirits.Clear();
        PlayerCounts.Clear();
        Difficulties.Clear();
        SpiritMode = MatchMode.Any;
    }

    public bool Matches(ListGamesResponse game)
    {
        var passesTab = Status switch
        {
            GameStatusTab.Victories => game is { IsCompleted: true, Win: true },
            GameStatusTab.Defeats => game is { IsCompleted: true, Win: false },
            GameStatusTab.Drafts => !game.IsCompleted,
            _ => true,
        };
        if (!passesTab) return false;

        if (!MatchesOrNone(Adversaries, game.Adversaries.Select(a => a.AdversaryId).ToList())) return false;
        if (!MatchesOrNone(Scenarios, game.ScenarioId is null ? [] : [game.ScenarioId])) return false;

        if (Spirits.Count > 0)
        {
            var atTable = game.Players.Select(p => p.SpiritId).ToList();
            if (!MatchesGroup(Spirits, atTable, SpiritMode)) return false;
        }

        if (PlayerCounts.Count > 0 && !PlayerCounts.Contains(game.PlayerCount)) return false;

        if (Difficulties.Count > 0
            && !AllDifficultyBands.Any(b => Difficulties.Contains(b.Key) && b.Contains(game.Difficulty)))
        {
            return false;
        }

        return MatchesQuery(game);
    }

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
