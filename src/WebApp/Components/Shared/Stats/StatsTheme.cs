namespace WebApp.Components.Shared.Stats;

/// <summary>
/// The app's palette as raw hex. Keeps these in sync with the Tailwind tokens (see
/// Styles/site.css) for the places that cannot take a CSS custom property: ApexCharts options,
/// which the class is named for, and anywhere a colour is concatenated rather than referenced —
/// a hex-alpha suffix, say, as the profile breakdowns' row tints do.
/// <para>
/// One home for that, because the redesign proved the alternative: three seat-owner swatches
/// spelled out in <see cref="Games.SetupProfiles"/> kept the pre-redesign emerald, sky and stone
/// when the tokens moved, under a comment promising they matched the assignee badge.
/// </para>
/// </summary>
public static class StatsTheme
{
    // Only the steps something actually reaches for — the full ramps live in site.css, and a
    // step is added here the day the first caller needs it.

    // accent — jade
    public const string Accent300 = "#74CCA6";
    public const string Accent500 = "#1FA877";
    public const string Accent600 = "#15875F";
    public const string Accent700 = "#0F6A4A";

    // ink — parchment neutrals. Grid lines and axis labels sit on a card, so they are
    // the light end of the ramp; anything carrying text is 500 or darker for contrast.
    public const string Ink200 = "#E3DAC6";
    public const string Ink300 = "#CFC2A5";
    public const string Ink400 = "#A89878";
    public const string Ink500 = "#7A6C4E";
    public const string Ink700 = "#4A4031";
    public const string Ink900 = "#221D16";

    // ember — the invaders. What a loss is drawn in.
    public const string DangerSoft = "#F4A98A"; // ember-300
    public const string Danger     = "#DC5B3C"; // ember-500

    // sun — score, difficulty, terror
    public const string Sun400 = "#E6C341";
    public const string Sun500 = "#D4AC1E";

    /// <summary>deep — the chrome, and what a friend's seat is marked in.</summary>
    public const string Sky600 = "#22608F";

    /// <summary>The app's paper white (--color-white in site.css) — light text and dot
    /// strokes on coloured chart fills.</summary>
    public const string Paper = "#FDFBF6";

    /// <summary>
    /// The diverging win-rate ramp: ember for losing records (red-700, red-400) through
    /// parchment at even to jade for winning ones. Equidistant stops — a cell colour is an
    /// interpolation along it and the legend swatch is a gradient built from it, so the two
    /// can never disagree.
    /// </summary>
    public static readonly IReadOnlyList<string> WinRateRamp =
        ["#B8412A", "#EE8560", "#D8CFC0", Accent300, Accent700];

    /// <summary>
    /// The ramp's stops parsed once, equidistant so an index maps straight to a rate. The legend
    /// draws the gradient from <see cref="WinRateRamp"/> and the cells interpolate along this, so
    /// the two cannot advertise different colours.
    /// </summary>
    private static readonly (double At, int R, int G, int B)[] WinRateStops =
        WinRateRamp
            .Select((hex, i) =>
            {
                var (r, g, b) = Rgb(hex);
                return (At: 100.0 * i / (WinRateRamp.Count - 1), R: r, G: g, B: b);
            })
            .ToArray();

    /// <summary>
    /// A win rate as a colour along the diverging ramp: ember (losing) → parchment (even) → jade
    /// (winning). The anchors darken monotonically away from the light midpoint, so depth of
    /// colour reads as distance from 50%. Lived in the heatmap that draws it; the ramp is the
    /// theme's, so the walk along it is too.
    /// </summary>
    public static string AlongWinRateRamp(double rate)
    {
        rate = Math.Clamp(rate, 0, 100);
        var hi = Array.FindIndex(WinRateStops, s => s.At >= rate);
        if (hi <= 0) return Hex(WinRateStops[0].R, WinRateStops[0].G, WinRateStops[0].B);
        var (a, b) = (WinRateStops[hi - 1], WinRateStops[hi]);
        var t = (rate - a.At) / (b.At - a.At);
        return Hex(
            (int)(a.R + (b.R - a.R) * t),
            (int)(a.G + (b.G - a.G) * t),
            (int)(a.B + (b.B - a.B) * t));
    }

    private static string Hex(int r, int g, int b) => $"#{r:X2}{g:X2}{b:X2}";

    public const string FontFamily = "Inter, system-ui, sans-serif";

    /// <summary>
    /// A <c>#rrggbb</c> hex as its three channels — for the two places that do arithmetic on a
    /// colour rather than pass it through: the contrast test behind <see cref="InkOn"/> and the
    /// interpolation along <see cref="WinRateRamp"/>. Both live here now, so nothing outside
    /// parses a hex.
    /// </summary>
    private static (int R, int G, int B) Rgb(string hex) =>
        (Convert.ToInt32(hex.Substring(1, 2), 16),
         Convert.ToInt32(hex.Substring(3, 2), 16),
         Convert.ToInt32(hex.Substring(5, 2), 16));

    /// <summary>
    /// The text colour a label needs on a coloured fill: dark ink on a light colour (Lightning's
    /// yellow drowns white text), paper on a dark one. Here with the palette it reads from rather
    /// than among the catalogue lookups, which have no business with luminance.
    /// </summary>
    public static string InkOn(string hexColor)
    {
        var (r, g, b) = Rgb(hexColor);
        var brightness = (0.299 * r + 0.587 * g + 0.114 * b) / 255;
        return brightness > 0.62 ? Ink900 : Paper;
    }
}
