using Domain.Models.Game;

namespace WebApp.Components.Shared.Stats;

/// <summary>
/// How a terror level is written and coloured wherever the statistics show one — the numeral on
/// a band's tile and the segment of a spirit's stacked bar. Two components drew the scale and one
/// of them reached into the other's <c>@code</c> for it; a palette decision belongs beside
/// <see cref="StatsTheme"/>, not on a view.
/// </summary>
public static class TerrorScale
{
    /// <summary>The level as the rules print it — I to IV.</summary>
    public static string Label(TerrorLevel level) => level switch
    {
        TerrorLevel.First  => "I",
        TerrorLevel.Second => "II",
        TerrorLevel.Third  => "III",
        TerrorLevel.Max    => "IV",
        _                  => "?",
    };

    /// <summary>
    /// Terror escalates from the island's own green, through the sun, into the invaders' ember —
    /// the palette's three semantic families in the order the game climbs them.
    /// </summary>
    public static string Color(TerrorLevel level) => level switch
    {
        TerrorLevel.First  => StatsTheme.Accent500,
        TerrorLevel.Second => StatsTheme.Sun400,
        TerrorLevel.Third  => StatsTheme.DangerSoft,
        TerrorLevel.Max    => StatsTheme.Danger,
        _                  => StatsTheme.Ink400,
    };

    /// <summary>The four levels in the order the game reaches them.</summary>
    public static readonly IReadOnlyList<TerrorLevel> Order =
    [
        TerrorLevel.First, TerrorLevel.Second, TerrorLevel.Third, TerrorLevel.Max,
    ];
}
