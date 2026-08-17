using Domain.Models.Static;

namespace WebApp.Components.Shared.Games.Filtering;

/// <summary>
/// The filter for a catalogue whose only questions are the shared ones — boards, adversaries,
/// scenarios, island layouts. Each is a short list of named things, so name, expansion, "have I
/// played it" and one number the thing happens to have is the whole of what there is to ask;
/// anything more would be controls with nothing behind them.
/// <para>
/// Spirits are the exception and have their own <see cref="SpiritFilter"/>: complexity, elements
/// and tokens give them real facets to sift by.
/// </para>
/// </summary>
public sealed class CatalogueFilter<T> : FilterState
{
    private readonly Func<T, string> _id;
    private readonly Func<T, string> _name;
    private readonly Func<T, ExpansionId>? _expansion;
    private readonly Func<T, int>? _number;

    /// <param name="expansion">
    /// Null for a catalogue that doesn't belong to expansions — island layouts are arrangements of
    /// boards, not things you buy.
    /// </param>
    /// <param name="numeric">
    /// The one number this catalogue has, and what to call it: an adversary's difficulty at its
    /// hardest, a layout's board count. It serves as both an ordering and, where a host chooses to
    /// render it, a set of pills — so "3 boards" and "sorted by boards" are the same axis rather
    /// than two things to keep in step.
    /// </param>
    public CatalogueFilter(
        Func<T, string> id,
        Func<T, string> name,
        Func<T, ExpansionId>? expansion = null,
        (SortOption Option, Func<T, int> Key)? numeric = null,
        string defaultSortKey = "expansion")
        : base(Options(expansion is not null, numeric?.Option), defaultSortKey)
    {
        _id = id;
        _name = name;
        _expansion = expansion;
        _number = numeric?.Key;
        NumberSort = numeric?.Option;
    }

    private static IReadOnlyList<SortOption> Options(bool hasExpansions, SortOption? number)
    {
        var options = new List<SortOption> { ByName };
        if (hasExpansions) options.Add(ByExpansion);
        if (number is not null) options.Add(number);
        options.Add(ByPlayed);
        options.Add(ByWinRate);
        return options;
    }

    public override bool HasExpansions => _expansion is not null;

    /// <summary>The numeric axis's sort option, when this catalogue has one.</summary>
    private SortOption? NumberSort { get; }

    /// <summary>Values of the numeric axis to keep; empty means all of them.</summary>
    public HashSet<int> Numbers { get; } = [];

    /// <summary>Every value the numeric axis actually takes, for the pills to offer.</summary>
    public IReadOnlyList<int> NumbersIn(IEnumerable<T> items) =>
        _number is null ? [] : items.Select(_number).Distinct().OrderBy(n => n).ToList();

    /// <summary>The expansions the items actually come from, for the pills to offer — boards stop
    /// at Horizons, so an owned expansion can still be a pill nothing answers to.</summary>
    public IReadOnlyList<ExpansionId> ExpansionsIn(IEnumerable<T> items) =>
        _expansion is null ? [] : items.Select(_expansion).Distinct().ToList();

    public override int ActiveCount => base.ActiveCount + Numbers.Count;

    public override void Clear()
    {
        base.Clear();
        Numbers.Clear();
    }

    private bool Matches(T item)
    {
        if (!MatchesShared(_id(item), _name(item), _expansion?.Invoke(item)))
            return false;

        return Numbers.Count == 0 || (_number is not null && Numbers.Contains(_number(item)));
    }

    /// <summary>The items that match, in the chosen order. Ties break by name, so the order is
    /// stable rather than incidental.</summary>
    public IReadOnlyList<T> Apply(IEnumerable<T> items)
    {
        var matched = items.Where(Matches);
        var ordered = SortKey switch
        {
            "expansion" when _expansion is not null => By(matched, i => ExpansionOrder(_expansion(i))),
            "played" => By(matched, i => Played(_id(i))),
            // An item with no finished game has no rate to compare, so it sits at the end whichever
            // way round the rest is — reversing the order shouldn't promote a blank to the top.
            "winrate" => Then(matched.OrderBy(i => Unrated(_id(i))), i => WinRate(_id(i))),
            _ when SortKey == NumberSort?.Key && _number is not null => By(matched, _number),
            _ => By(matched, _name, StringComparer.CurrentCulture),
        };
        return ordered.ThenBy(_name, StringComparer.CurrentCulture).ToList();
    }
}
