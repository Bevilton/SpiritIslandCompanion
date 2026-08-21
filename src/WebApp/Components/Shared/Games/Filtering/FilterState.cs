using System.Diagnostics.CodeAnalysis;
using Domain.Models.Static;

namespace WebApp.Components.Shared.Games.Filtering;

/// <summary>Whether a multi-pick group means "has one of these" or "has all of these".</summary>
public enum MatchMode
{
    All,
    Any,
}

/// <summary>
/// One way a catalogue can be ordered, and what its two ends are called.
/// <para>
/// The direction words live here rather than being "ascending / descending" because those are
/// meaningless for most of these — nobody reads "descending play count" as "the ones I play
/// most". <paramref name="StartsDescending"/> is the end the field is usually wanted from, so
/// picking it lands on the useful answer straight away.
/// </para>
/// </summary>
public sealed record SortOption(
    string Key,
    string Label,
    string Ascending,
    string Descending,
    bool StartsDescending = false)
{
    /// <summary>Needs a play history to order by; hidden without one, where every row would tie.</summary>
    public bool NeedsHistory { get; init; }
}

/// <summary>
/// What every catalogue filter has in common: a name search, the expansions in play, whether to
/// show only what the player hasn't tried, and the ordering. Spirits, boards, adversaries and
/// scenarios all ask these, so they are asked once here and rendered once by <c>FilterBar</c>;
/// each catalogue adds only what is peculiar to it.
/// </summary>
public abstract class FilterState
{
    protected FilterState(IReadOnlyList<SortOption> sortOptions, string defaultSortKey)
    {
        SortOptions = sortOptions;
        DefaultSortKey = defaultSortKey;
        SetSort(defaultSortKey);
    }

    public IReadOnlyList<SortOption> SortOptions { get; }
    public string DefaultSortKey { get; }

    public string Query { get; set; } = "";

    /// <summary>Expansion ids to keep. Always "any of" — an item belongs to exactly one.</summary>
    public HashSet<string> Expansions { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Whether this catalogue belongs to expansions at all. Island layouts don't — they are
    /// arrangements of boards, not things you buy — so the row and the ordering are left out
    /// rather than shown with nothing behind them.
    /// </summary>
    public virtual bool HasExpansions => true;

    /// <summary>Only what the player has never tried. Off unless there is a history to ask.</summary>
    public bool UntestedOnly { get; set; }

    public string SortKey { get; private set; }
    public bool Descending { get; set; }

    /// <summary>
    /// Counts the times an order has been picked through <see cref="SetSort"/>, including
    /// re-picking the one already active. A host layering its own sorts on top (the statistics
    /// table) watches this to tell "the filter's order was asked for again" apart from an
    /// unrelated change — the key/direction pair alone can't say, since re-picking reproduces
    /// it exactly.
    /// </summary>
    public int SortEpoch { get; private set; }

    /// <summary>
    /// Whether an item counts as already tried, for <see cref="UntestedOnly"/>. Supplied by the
    /// host because it is not always the same question as <see cref="Records"/> answers: a picker
    /// opened against a chosen adversary asks "tried against this foe", while the ordering below
    /// wants the overall record.
    /// </summary>
    public Func<string, bool>? IsTested { get; set; }

    /// <summary>Overall record per item id, when the caller has one — drives the play orderings.</summary>
    public IReadOnlyDictionary<string, SetupInsights.PlayRecord>? Records { get; set; }

    /// <summary>
    /// Wires a play history in: <paramref name="records"/> drives the play orderings, and
    /// "tried" defaults to "played at least once" unless the host asks a narrower question
    /// via <paramref name="isTested"/>.
    /// </summary>
    public void UseHistory(
        IReadOnlyDictionary<string, SetupInsights.PlayRecord>? records,
        Func<string, bool>? isTested = null)
    {
        Records = records;
        IsTested = isTested ?? (records is null ? null : id => records.GetValueOrDefault(id) is { Played: > 0 });
    }

    public bool HasRecords => Records is { Count: > 0 };
    public bool HasHistory => HasRecords || IsTested is not null;

    /// <summary>The orderings worth offering — the play-based ones need a history.</summary>
    public IEnumerable<SortOption> AvailableSorts =>
        SortOptions.Where(o => !o.NeedsHistory || HasRecords);

    public SortOption CurrentSort =>
        SortOptions.FirstOrDefault(o => o.Key == SortKey) ?? SortOptions[0];

    /// <summary>
    /// Changes the field and starts it from whichever end that field is usually wanted from.
    /// Keeping the previous direction would mean picking "played" and being shown what you have
    /// played least, which is not what the click was asking for.
    /// </summary>
    [MemberNotNull(nameof(SortKey))]
    public void SetSort(string key)
    {
        SortKey = key;
        Descending = SortOptions.FirstOrDefault(o => o.Key == key)?.StartsDescending ?? false;
        SortEpoch++;
    }

    /// <summary>Whether the current order still follows the expansions, so a host can group by them.</summary>
    public bool GroupsByExpansion => SortKey == ByExpansion.Key;

    /// <summary>Whether the order has been moved off the one the host opened with.</summary>
    public bool SortChanged =>
        SortKey != DefaultSortKey
        || Descending != (SortOptions.FirstOrDefault(o => o.Key == DefaultSortKey)?.StartsDescending ?? false);

    /// <summary>How the current order reads in one phrase, for the collapsed filter header.</summary>
    public string SortSummary =>
        $"{CurrentSort.Label.ToLowerInvariant()}, " +
        $"{(Descending ? CurrentSort.Descending : CurrentSort.Ascending).ToLowerInvariant()} first";

    /// <summary>Pills currently narrowing the list. The free-text query is not one of them.</summary>
    public virtual int ActiveCount => Expansions.Count + (UntestedOnly ? 1 : 0);

    public bool Any => ActiveCount > 0;

    public virtual void Clear()
    {
        Expansions.Clear();
        UntestedOnly = false;
    }

    /// <summary>Resets everything, including the search box and the ordering.</summary>
    public void Reset()
    {
        Clear();
        Query = "";
        ResetExtras();
        SetSort(DefaultSortKey);
    }

    /// <summary>Hook for a catalogue's own switches, which <see cref="Clear"/> leaves alone.</summary>
    protected virtual void ResetExtras() { }

    /// <summary>The checks every catalogue shares — name, expansion, and never-tried.</summary>
    protected bool MatchesShared(string id, string name, ExpansionId? expansion)
    {
        if (!string.IsNullOrWhiteSpace(Query) && !name.Contains(Query, StringComparison.OrdinalIgnoreCase))
            return false;

        if (Expansions.Count > 0 && (expansion is null || !Expansions.Contains(expansion.Value)))
            return false;

        if (UntestedOnly && IsTested is { } tested && tested(id))
            return false;

        return true;
    }

    public static void Toggle<T>(HashSet<T> set, T value)
    {
        if (!set.Add(value)) set.Remove(value);
    }

    protected static bool MatchesGroup<T>(HashSet<T> selected, IReadOnlyList<T> has, MatchMode mode) =>
        selected.Count == 0
        || (mode == MatchMode.All ? selected.All(has.Contains) : has.Any(selected.Contains));

    protected IOrderedEnumerable<T> By<T, TKey>(
        IEnumerable<T> source, Func<T, TKey> key, IComparer<TKey>? comparer = null) =>
        Descending ? source.OrderByDescending(key, comparer) : source.OrderBy(key, comparer);

    protected IOrderedEnumerable<T> Then<T, TKey>(IOrderedEnumerable<T> source, Func<T, TKey> key) =>
        Descending ? source.ThenByDescending(key) : source.ThenBy(key);

    /// <summary>Release order, as the catalogue lists them — not alphabetical, which would put
    /// Jagged Earth before the base game.</summary>
    protected static int ExpansionOrder(ExpansionId id)
    {
        var all = Domain.Models.Static.Data.GameData.Expansions;
        for (var i = 0; i < all.Count; i++)
            if (all[i].Id == id) return i;
        return int.MaxValue;
    }

    protected SetupInsights.PlayRecord? Record(string id) => Records?.GetValueOrDefault(id);
    protected int Played(string id) => Record(id)?.Played ?? 0;
    protected bool Unrated(string id) => Record(id) is not { Completed: > 0 };
    protected double WinRate(string id) => Record(id)?.WinRate ?? 0;

    /// <summary>The orderings every catalogue shares, for a concrete filter's option list.</summary>
    public static readonly SortOption ByName = new("name", "Name", "A–Z", "Z–A");
    public static readonly SortOption ByExpansion = new("expansion", "Expansion", "Oldest", "Newest");
    public static readonly SortOption ByDifficulty = new("difficulty", "Difficulty", "Easiest", "Hardest");
    public static readonly SortOption ByPlayed = new("played", "Played", "Fewest", "Most", StartsDescending: true) { NeedsHistory = true };
    public static readonly SortOption ByWinRate = new("winrate", "Win rate", "Worst", "Best", StartsDescending: true) { NeedsHistory = true };
}
