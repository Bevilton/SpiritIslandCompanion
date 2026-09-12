namespace WebApp.Components.Shared;

/// <summary>
/// The wording the whole UI shares — the phrases that are written out at more call sites than
/// can be kept in step by hand.
/// <para>
/// English plurals are the bulk of it: <c>@count game@(count == 1 ? "" : "s")</c> appeared about
/// forty times, in markup where the noun is the only part that reads and the ternary is four
/// fifths of the line.
/// </para>
/// <para>
/// Deliberately not a general inflector. It covers exactly the nouns this app counts: most take a
/// plain -s (game, player, board, spirit, layout, aspect, draft, level, expansion, scenario), a
/// couple take -es (match), and a couple swap -y for -ies (adversary, victory). Anything
/// irregular should be spelled out at the call site rather than taught to this.
/// </para>
/// </summary>
public static class Copy
{
    /// <summary>"1 game", "3 games", "1 adversary", "8 adversaries".</summary>
    public static string Count(int count, string noun) => $"{count} {Noun(count, noun)}";

    /// <summary>The noun alone, in the number the count calls for — for the cases where the
    /// count is rendered separately because it is styled differently.</summary>
    public static string Noun(int count, string noun)
    {
        if (count == 1) return noun;
        if (noun.EndsWith('y') && noun.Length > 1 && !"aeiou".Contains(noun[^2]))
        {
            return $"{noun[..^1]}ies";
        }
        return noun.EndsWith("ch", StringComparison.Ordinal) || noun.EndsWith('s')
            ? $"{noun}es"
            : $"{noun}s";
    }

    /// <summary>
    /// What a filtered list says once the filters have left it with nothing. Nine call sites in
    /// nine files were spelling the sentence out — the four browse views, the four pickers and
    /// the games list — which is nine chances for one catalogue to start phrasing a dead end
    /// differently from the one beside it. The <c>.no-matches</c> class carries how it looks and
    /// this carries what it says; the surface around it stays the caller's, since some of these
    /// sit in a card of their own and some in a table cell.
    /// </summary>
    /// <param name="plural">What the list holds, in the plural — "spirits", "adversaries".</param>
    public static string NoMatches(string plural) => $"No {plural} match the current filters.";

    /// <summary>
    /// The same dead end on a statistics tab, where the filtered list counts only what has been
    /// played and the never-played are named in the coverage panel below it — so an empty list
    /// there has somewhere to point rather than looking like the whole catalogue vanished.
    /// </summary>
    /// <param name="verb">How this catalogue says "played" — a foe is "faced".</param>
    public static string NoRecordedMatches(string plural, string verb = "played") =>
        $"No {verb} {plural} match — clear a filter, or see the coverage panel below for the never-{verb}.";

    /// <summary>
    /// The tooltip on a filter bar's never-played switch. Seven of the eleven bars in the app
    /// spelled this sentence out — the four browse views, two of the pickers and the layouts
    /// list — and the two that say it about the same catalogue (a picker and the browse page
    /// behind it) sit in different folders, so nothing would have caught them drifting apart.
    /// </summary>
    /// <param name="plural">What the list holds, capitalised — "Spirits", "Adversaries".</param>
    /// <param name="verb">How this catalogue says "played" — a foe is "faced", a board "used".</param>
    public static string Untried(string plural, string verb = "played") =>
        $"{plural} nobody at your table has {verb}";

    /// <summary>
    /// The same switch on a statistics tab, where the list is only what has been played — so the
    /// switch would empty the tab rather than narrow it, and has to point at the coverage panel
    /// that does name the never-played. The twin of <see cref="NoRecordedMatches"/>, which is the
    /// sentence the emptied list itself then carries.
    /// </summary>
    /// <param name="verb">What the catalogue has never had done to it — "taken", "met", "tried".</param>
    /// <param name="counts">What the tab counts, where that is a different word — "played".</param>
    public static string UntriedOnStatsTab(string plural, string verb, string counts = "played") =>
        $"Only {plural} nobody at your table has {verb} — this tab counts the {counts}, "
        + "so the coverage panel below is where they are named";

    /// <summary>
    /// Whose record a heading is about, as the possessive that goes in front of it — "Anna's",
    /// "your", or null where the numbers are the whole account's and belong to nobody in
    /// particular. The same three-way decision was written out at four call sites (the level
    /// ladder's heading, the variant strip's, the board picker's line about whose counts these
    /// are, and the profile section's own), which is four places for one of them to start saying
    /// "your" where its neighbour says nothing.
    /// </summary>
    /// <param name="scoped">
    /// Whether the numbers are narrowed to one person at all. With no <paramref name="subjectName"/>
    /// that person is the reader, which is what makes the answer "your" rather than a name.
    /// </param>
    public static string? Owner(string? subjectName, bool scoped) =>
        subjectName is { } name ? $"{name}'s" : scoped ? "your" : null;

    /// <summary>
    /// What anything that opens a subject's reference sheet says on hover — the record pills, the
    /// coverage panels' missing pills, the statistics tables' rows, the insight cards, the
    /// landscape chart's dots. Twelve call sites in six files were each spelling this sentence
    /// out, and several of them sit side by side on one statistics tab.
    /// </summary>
    /// <param name="hasRecord">
    /// False where the sheet has no history to show — the public reference pages, which have
    /// nobody to have one. Promising a record there and then not showing one is worse than not
    /// mentioning it.
    /// </param>
    public static string SheetTooltip(string subject, bool hasRecord = true) =>
        hasRecord ? $"{subject} — details and full record" : $"{subject} — details";
}
