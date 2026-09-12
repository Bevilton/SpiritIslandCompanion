namespace WebApp.Components.Shared.Games;

/// <summary>
/// How an outcome reads in colour, in one place: jade for the island's side of it, ember for the
/// invaders'. For a rate that is the 50% threshold, so every record pill and level strip turns
/// together; for a game already won or lost it is the pair of tones a result panel is built from.
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
    /// Tone for one game's outcome, where a figure or a line is about a single result rather
    /// than a rate — a score in a games row, the "last time you lost" line on the setup screen.
    /// The same pair as <see cref="ForRate"/>, so a won game and a winning record read alike;
    /// three lists had each picked their own step of the two ramps.
    /// </summary>
    public static string ForOutcome(bool win) => win ? "text-accent-700" : "text-red-600";

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
    /// The same decision as a raw colour, for a bar, a ring or a swatch that can't take a
    /// Tailwind class — the twin of <see cref="ForRate"/>. For a caller that has already
    /// established there is something finished behind the rate; <see cref="FillFor"/> is this one
    /// with the empty record handled.
    /// </summary>
    public static string Fill(double winRate) =>
        winRate >= 50 ? "var(--color-accent-500)" : "var(--color-ember-500)";

    /// <summary>
    /// <see cref="Fill"/> with the empty record handled. <paramref name="completed"/> of zero is
    /// neutral rather than a loss — a band nobody has finished a game in is unknown, and painting
    /// it as ember would report a defeat that never happened.
    /// </summary>
    public static string FillFor(double winRate, int completed) =>
        completed == 0 ? "var(--color-ink-300)" : Fill(winRate);

    /// <summary>
    /// The caption over a figure on a .panel-result — the word "Score", "Outcome". Its pair is
    /// <see cref="OutcomeFigure"/>.
    /// <para>
    /// The score preview a result is entered against and the saved result it becomes are the same
    /// panel on two screens, and each had the ternary written out at four points of its own band.
    /// One decision, so the two cannot start disagreeing about what a defeat looks like.
    /// </para>
    /// </summary>
    public static string OutcomeCaption(bool win) => win ? "text-accent-700" : "text-red-700";

    /// <summary>The figure itself on a result panel — the score, the word Victory or Defeat.</summary>
    public static string OutcomeFigure(bool win) => win ? "text-accent-900" : "text-red-900";

    /// <summary>
    /// The dark gradient a recorded game's summary panel is headed with: jade for a win, ember for
    /// a loss, and the deep ramp for a draft with no outcome yet — deep rather than ink, because
    /// ink is the parchment family and read as brown beside the teal sidebar. The one three-way
    /// outcome decision in the app, kept here with the two-way ones rather than as a class
    /// ternary on the page.
    /// </summary>
    public static string DeepPanel(bool? win) => win switch
    {
        true  => "bg-gradient-to-br from-accent-700 to-accent-900 text-white",
        false => "bg-gradient-to-br from-red-700 to-red-900 text-white",
        null  => "bg-gradient-to-br from-deep-700 to-deep-900 text-white",
    };
}
