using System.Globalization;

namespace WebApp.Components.Shared;

/// <summary>
/// A number on its way into markup the browser parses rather than into text a reader reads — a
/// CSS length, a custom property, an SVG coordinate, a dash array.
/// <para>
/// These must be spelled with a decimal point whatever culture the server happens to be running
/// under: a comma is not a parse error the browser reports, it is an attribute silently dropped,
/// so the fill has no width, the ring has no radius and the ships all render full size. It is
/// invisible on an English machine and total on a Czech one, which is exactly the kind of thing
/// nobody finds by looking at the page.
/// </para>
/// <para>
/// Five places were writing these by hand and had settled it four different ways: a
/// <c>CultureInfo</c> field on the landing page, a <c>Num</c> helper in one diagram, an <c>S</c>
/// helper in another, and the whole
/// <c>System.Globalization.CultureInfo.InvariantCulture</c> spelled out inline in the meter. Two
/// more — the win-rate ring's own geometry and the terror bar's segment widths — had simply been
/// missed, which is what four spellings of one rule buys you.
/// </para>
/// </summary>
public static class BrowserNumber
{
    /// <summary>
    /// The value as the browser must read it. <paramref name="format"/> is the caller's, since
    /// how many decimals a number is worth is the caller's question: a bar's width rounds to
    /// whole per cent, an SVG coordinate keeps one, a dash array two. Omit it for the value in
    /// full.
    /// </summary>
    public static string ToInvariant(this double value, string? format = null) =>
        value.ToString(format, CultureInfo.InvariantCulture);
}
