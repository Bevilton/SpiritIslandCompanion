namespace WebApp.Components.Shared.Games;

/// <summary>
/// The single decision for how a win rate reads: green from 50% up, red below.
/// One home for the threshold so every record pill and level strip turns together.
/// </summary>
public static class WinTone
{
    /// <summary>Tone for a rate with completed games behind it.</summary>
    public static string ForRate(double winRate) =>
        winRate >= 50 ? "text-accent-700" : "text-red-600";

    /// <summary>Tone for a whole record: muted while nothing has finished, the rate's tone after.</summary>
    public static string ForRecord(SetupInsights.PlayRecord? record) =>
        record is not { Completed: > 0 } ? "text-ink-400" : ForRate(record.WinRate);

    /// <summary>
    /// The same decision as a badge variant, for a rate shown as a pill rather than as text.
    /// <paramref name="completed"/> of zero is neutral for the same reason as in
    /// <see cref="FillFor"/>: a spirit whose games are all still drafts has a win rate of 0 by
    /// arithmetic, and a red 0% pill would report a run of defeats that never happened.
    /// </summary>
    public static string BadgeFor(double winRate, int completed) =>
        completed == 0 ? "badge-neutral"
        : winRate >= 50 ? "badge-success"
        : "badge-danger";

    /// <summary>
    /// The same decision as a raw colour, for a bar or a swatch that can't take a Tailwind class.
    /// <paramref name="completed"/> of zero is neutral rather than a loss — a band nobody has
    /// finished a game in is unknown, and painting it as ember would report a defeat that never
    /// happened.
    /// </summary>
    public static string FillFor(double winRate, int completed) =>
        completed == 0 ? "var(--color-ink-300)"
        : winRate >= 50 ? "var(--color-accent-500)"
        : "var(--color-ember-500)";
}
