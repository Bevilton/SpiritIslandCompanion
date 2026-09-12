using Domain.Models.Static;
using Domain.Models.Static.Data;

namespace WebApp.Components.Shared.Games.Filtering;

/// <summary>
/// The spirit filter. Spirits are the one catalogue with real facets of their own — complexity,
/// elements and tokens — so they extend the shared questions in <see cref="FilterState"/> rather
/// than using <see cref="CatalogueFilter{T}"/> as boards, adversaries and scenarios do.
/// <para>
/// Elements and tokens default to <see cref="MatchMode.All"/>. Picking Fire and Air almost always
/// means a spirit with both; "any" is still a click away for when it doesn't. Expansion and
/// complexity are always "any", because a spirit has exactly one of each and "all" would match
/// nothing.
/// </para>
/// </summary>
public sealed class SpiritFilter : FilterState
{
    private static readonly SortOption ByComplexity =
        new("complexity", "Complexity", "Simplest", "Hardest");

    private static readonly SortOption[] Sorts =
        [ByName, ByExpansion, ByComplexity, ByPlayed, ByWinRate];

    public SpiritFilter(string? defaultSortKey = null) : base(Sorts, defaultSortKey ?? ByExpansion.Key) { }

    public static readonly Complexity[] AllComplexities =
        [Complexity.Low, Complexity.Moderate, Complexity.High, Complexity.VeryHigh];

    public static readonly Element[] AllElements =
        [Element.Sun, Element.Moon, Element.Fire, Element.Air, Element.Water, Element.Earth, Element.Plant, Element.Animal];

    public static readonly Token[] AllTokens =
        [Token.Dahan, Token.Beasts, Token.Wilds, Token.Disease, Token.Strife, Token.Badlands];

    public HashSet<Complexity> Complexities { get; } = [];
    public HashSet<Element> Elements { get; } = [];
    public HashSet<Token> Tokens { get; } = [];

    public MatchMode ElementMode { get; set; } = MatchMode.All;
    public MatchMode TokenMode { get; set; } = MatchMode.All;

    public override int ActiveCount =>
        base.ActiveCount + Complexities.Count + Elements.Count + Tokens.Count;

    public override void Clear()
    {
        base.Clear();
        Complexities.Clear();
        Elements.Clear();
        Tokens.Clear();
    }

    protected override void ResetExtras()
    {
        ElementMode = MatchMode.All;
        TokenMode = MatchMode.All;
    }

    private bool Matches(Spirit spirit)
    {
        if (!MatchesShared(spirit.Id.Value, spirit.Name, spirit.ExpansionId))
            return false;

        var detail = SpiritDetails.For(spirit.Id);
        if (detail is null)
            // Nothing to match the catalogue facets against, so it only survives while none are set.
            return Complexities.Count == 0 && Elements.Count == 0 && Tokens.Count == 0;

        if (Complexities.Count > 0 && !Complexities.Contains(detail.Complexity))
            return false;

        return MatchesGroup(Elements, detail.Elements, ElementMode)
            && MatchesGroup(Tokens, detail.Tokens, TokenMode);
    }

    /// <summary>The expansions the spirits actually come from, for the pills to offer — the same
    /// question <see cref="CatalogueFilter{T}.ExpansionsIn"/> answers, for hosts listing a subset
    /// (the statistics page offers only the played).</summary>
    public IReadOnlyList<ExpansionId> ExpansionsIn(IEnumerable<Spirit> spirits) =>
        spirits.Select(s => s.ExpansionId).Distinct().ToList();

    /// <summary>The spirits that match, in the chosen order. Ties break by name, so the order is
    /// stable rather than incidental.</summary>
    public IReadOnlyList<Spirit> Apply(IEnumerable<Spirit> spirits)
    {
        var matched = spirits.Where(Matches);
        var ordered =
            SharedOrder(matched, s => s.Id.Value, s => s.ExpansionId)
            ?? (SortKey == ByComplexity.Key
                // A spirit with no catalogue entry has no complexity to compare, so it sits at the
                // end whichever way round the rest is — the same rule SharedOrder applies to a
                // spirit with no finished game.
                ? Then(matched.OrderBy(s => SpiritDetails.For(s.Id) is null),
                    s => (int?)SpiritDetails.For(s.Id)?.Complexity ?? 0)
                : By(matched, s => s.Name, StringComparer.CurrentCulture));
        return ordered.ThenBy(s => s.Name, StringComparer.CurrentCulture).ToList();
    }

    public static string ComplexityLabel(Complexity c) => c switch
    {
        Complexity.Low => "Low",
        Complexity.Moderate => "Moderate",
        Complexity.High => "High",
        Complexity.VeryHigh => "Very high",
        _ => c.ToString(),
    };
}
