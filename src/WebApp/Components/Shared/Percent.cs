namespace WebApp.Components.Shared;

/// <summary>
/// A part of a whole as a percentage, with the empty whole answered once. Every share the app
/// draws or prints comes through here — a win rate over what finished, a coverage over what is
/// owned, a bar's length against the busiest band, a leaderboard row against its leader.
/// <para>
/// Twelve sites had the same two lines written out, in four spellings (<c>(double)a / b * 100</c>,
/// <c>100.0 * a / b</c>, <c>a * 100.0 / b</c>, <c>a / (double)b * 100</c>) and with the zero guard
/// present on some and left to the caller's own <c>if</c> on others. A division by nothing is
/// <c>NaN</c>, which a width attribute drops silently and a <c>:F0</c> prints as "NaN" — so the
/// guard belongs to the arithmetic, not to whoever remembers it.
/// </para>
/// <para>
/// Nought over nought is nought: a record with nothing finished has no rate, and every reader of
/// one already says so in words (<c>WinRateFigure</c>'s dash, <c>WinRateRing</c>'s empty ring)
/// rather than trusting the figure. The value is not clamped — <c>Meter</c> clamps its own width,
/// and a share arithmetic can push past a hundred is a fact the caller may want to see.
/// </para>
/// </summary>
public static class Percent
{
    /// <summary><paramref name="part"/> as a percentage of <paramref name="whole"/>, 0–100 for
    /// any sane pair; 0 where there is no whole to be a part of.</summary>
    public static double Of(int part, int whole) => whole > 0 ? 100.0 * part / whole : 0;
}
