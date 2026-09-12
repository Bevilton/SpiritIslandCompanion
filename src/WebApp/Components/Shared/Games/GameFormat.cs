namespace WebApp.Components.Shared.Games;

/// <summary>
/// How a recorded game's own numbers are written. Both of these were defined twice — the signed
/// figure in <c>GameResultEditor</c> and <c>GameResultSummary</c>, the duration in
/// <c>GameResultSummary</c> and <c>GameDetail</c> — as identical private statics, which meant the
/// score preview and the saved result were free to start disagreeing about what "+3" or "1 h 30
/// min" looks like.
/// <para>
/// Distinct from <see cref="Stats.StatsFormat.Hours"/>, which spells a duration for a statistic
/// ("6h", "4d 3h"): that one is a total spanning days and is read at a glance, this one is one
/// evening and is read exactly.
/// </para>
/// </summary>
public static class GameFormat
{
    /// <summary>A score contribution, with its sign always shown — "+10", "−0" never, "-3".</summary>
    public static string Signed(int value) => value >= 0 ? $"+{value}" : value.ToString();

    /// <summary>How long one game took: "45 min", "2 h", "1 h 30 min".</summary>
    public static string Duration(TimeSpan span)
    {
        if (span.TotalHours < 1) return $"{(int)span.TotalMinutes} min";
        var hours = (int)span.TotalHours;
        return span.Minutes == 0 ? $"{hours} h" : $"{hours} h {span.Minutes} min";
    }

    /// <summary>
    /// When a game happened, at the app's usual precision — "Aug 31, 2026". Four surfaces
    /// (the setup review, a person's last-played line, a merge request's game list, the form
    /// strip's tooltips) each spelled the pattern out, which is how the recent-games list came
    /// to zero-pad its day on one page and not the other.
    /// </summary>
    public static string Date(DateTime when) => when.ToString("MMM d, yyyy");

    /// <summary>The day alone — "Aug 31" — for a line already inside a dated context.</summary>
    public static string DayOnly(DateTime when) => when.ToString("MMM d");

    /// <summary>
    /// The date spelled out — "August 31, 2026". What a surface showing an abbreviated month
    /// expands to on hover: the date tile reads "31 / AUG", so its tooltip is the long form
    /// rather than the same abbreviation again.
    /// </summary>
    public static string LongDate(DateTime when) => when.ToString("MMMM d, yyyy");

    /// <summary>The full moment — "August 31, 2026 · 14:30" — where the game's own page states
    /// it as a fact rather than riding it in a row.</summary>
    public static string Timestamp(DateTime when) => $"{LongDate(when)} · {TimeOfDay(TimeOnly.FromDateTime(when))}";

    /// <summary>The clock alone — "14:30" — where the date is said beside it another way, as the
    /// setup form's summary badge does. The same spelling <see cref="Timestamp"/> ends with.</summary>
    public static string TimeOfDay(TimeOnly when) => when.ToString("HH:mm");

    /// <summary>A month as a heading over the games in it — "August 2026".</summary>
    public static string Month(DateTime when) => when.ToString("MMMM yyyy");

    /// <summary>The same month at tick size — "Aug 26" — along a chart's axis, where the
    /// heading form would run its neighbours together.</summary>
    public static string MonthShort(DateTime when) => when.ToString("MMM yy");
}
