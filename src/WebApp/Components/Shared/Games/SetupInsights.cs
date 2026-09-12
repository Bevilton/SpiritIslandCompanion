using Application.Features.Statistics;

namespace WebApp.Components.Shared.Games;

/// <summary>
/// In-memory tallies over the <see cref="SetupGameFact"/> history a page loads once: how often
/// each spirit, foe, board, scenario and island was played and how those games went, plus the
/// matchup views the setup screen's pickers read. Every surface that reports a record — the
/// setup screen, the reference pages, the statistics page and the record sheets they open — counts
/// through here, so "a draft counts as played but not as a result" is decided once. A personal
/// history is at most a few hundred rows; the hosts that re-render often cache what they derive
/// from it, and the rest recompute on the change that needs it.
/// </summary>
public static class SetupInsights
{
    /// <summary>An adversary chosen on the form. Level null = "any level".</summary>
    public sealed record AdversaryPick(string AdversaryId, int? Level);

    /// <summary>
    /// The setup currently on the form, as the pickers need to see it: the loaded
    /// history, what the stats are measured against (picked adversaries/scenario),
    /// and whose seat is being edited. Pickers derive their own numbers from this
    /// rather than having each statistic passed in separately.
    /// </summary>
    public sealed record MatchupContext(
        IReadOnlyList<SetupGameFact> Facts,
        IReadOnlyList<AdversaryPick> Adversaries,
        string? ScenarioId,
        Func<SetupFactSeat, bool>? SeatFilter = null,
        string? SubjectName = null)
    {
        /// <summary>No history — pickers then show catalogue data only.</summary>
        public static readonly MatchupContext Empty = new([], [], null);

        public bool HasHistory => Facts.Count > 0;

        /// <summary>Whether anything is selected for the stats to be measured against.</summary>
        public bool HasSelection => Adversaries.Count > 0 || !string.IsNullOrEmpty(ScenarioId);

        /// <summary>The exact selection, e.g. "vs England L3 · Blitz"; null when nothing is picked.</summary>
        public string? ExactLabel
        {
            get
            {
                var parts = new List<string>();
                if (Adversaries.Count == 1 && GameLookups.AdversaryFor(Adversaries[0].AdversaryId) is { } adv)
                    parts.Add($"vs {adv.Name}{(Adversaries[0].Level is { } level ? $" L{level}" : "")}");
                else if (Adversaries.Count > 1)
                    parts.Add("vs this matchup");
                if (GameLookups.ScenarioFor(ScenarioId) is { } scenario)
                    parts.Add(parts.Count > 0 ? scenario.Name : $"in {scenario.Name}");
                return parts.Count == 0 ? null : string.Join(" · ", parts);
            }
        }

        /// <summary>The picked adversaries alone, any level — "vs England".</summary>
        public string? BroadLabel => Adversaries.Count switch
        {
            1 => GameLookups.AdversaryFor(Adversaries[0].AdversaryId) is { } adv ? $"vs {adv.Name}" : null,
            > 1 => "vs these foes",
            _ => null
        };

        /// <summary>The level picked for a single adversary — highlighted in per-level strips.</summary>
        public int? HighlightLevel => Adversaries is [{ } single] ? single.Level : null;

        /// <summary>
        /// Who the untested pill's counts belong to — "«name» has never «played»", "you have
        /// never…", or the whole table while the seat is still anyone's. The spirit and board
        /// pickers each carried this three-way switch, differing only in the verb.
        /// </summary>
        public string UntestedIntro(string verb) =>
            SubjectName is { } name ? $"{name} has never {verb}"
            : SeatFilter is not null ? $"you have never {verb}"
            : $"nobody at your table has {verb}";

        /// <summary>The concession the untested pill makes — a spirit played twenty times but
        /// never against these foes still passes it.</summary>
        public string UntestedConcession(string verb) =>
            SubjectName is { } name ? $"however often {name} has {verb} them otherwise"
            : SeatFilter is not null ? $"however often you have {verb} them otherwise"
            : $"however often they have been {verb} otherwise";

        /// <summary>Games matching the selection as it stands — picked levels and scenario included.</summary>
        public IReadOnlyList<SetupGameFact> ExactGames() => Filter(Facts, Adversaries, ScenarioId);

        /// <summary>The same foes at any level, in any scenario — the widest "related" history.</summary>
        public IReadOnlyList<SetupGameFact> BroadGames() =>
            Filter(Facts, Adversaries, ScenarioId, matchLevels: false, matchScenario: false);

        /// <summary>The same foes at any level, still in the picked scenario.</summary>
        public IReadOnlyList<SetupGameFact> AnyLevelGames() =>
            Filter(Facts, Adversaries, ScenarioId, matchLevels: false);

        public Dictionary<string, SpiritMatchup> SpiritMatchups() =>
            SetupInsights.SpiritMatchups(Facts, Adversaries, ScenarioId, SeatFilter);

        public Dictionary<string, PlayRecord> SpiritRecords() =>
            SetupInsights.SpiritRecords(Facts, SeatFilter);

        /// <summary>Per-aspect record for one spirit — see <see cref="SetupInsights.AspectRecords"/>.</summary>
        public Dictionary<string, PlayRecord> AspectRecords(string? spiritId) =>
            SetupInsights.AspectRecords(Facts, string.IsNullOrEmpty(spiritId) ? null : spiritId, SeatFilter);

        /// <summary>Every spirit's aspect records at once — see <see cref="SetupInsights.AspectRecordsBySpirit"/>.</summary>
        public Dictionary<string, Dictionary<string, PlayRecord>> AspectRecordsBySpirit() =>
            SetupInsights.AspectRecordsBySpirit(Facts, SeatFilter);

        /// <summary>Per-board record, scoped to one spirit when <paramref name="spiritId"/> is set.</summary>
        public Dictionary<string, PlayRecord> BoardRecords(string? spiritId = null) =>
            SetupInsights.BoardRecords(Facts, string.IsNullOrEmpty(spiritId) ? null : spiritId, SeatFilter);

        public Dictionary<string, AdversaryFacing> AdversaryRecords() =>
            SetupInsights.AdversaryRecords(Facts);

        public Dictionary<string, PlayRecord> ScenarioRecords() =>
            SetupInsights.ScenarioRecords(Facts);
    }

    /// <summary>The user's record against one specific table setup.</summary>
    public sealed record MatchupRecord(
        int Played,
        int Completed,
        int Wins,
        int Losses,
        double WinRate,
        int? BestScore,
        DateTimeOffset? LastPlayed,
        bool? LastWin);

    /// <summary>Play/win/loss tally for one spirit, board, or scenario inside a set of games.</summary>
    public sealed record PlayRecord(int Played, int Wins, int Losses)
    {
        public int Completed => Wins + Losses;
        public double WinRate => Percent.Of(Wins, Completed);
    }

    /// <summary>A server-side statistic in the shape every record pill, level strip and filter
    /// reads. Private: every caller wants a whole set keyed for lookup, which is
    /// <see cref="PlayRecordsBy"/>.</summary>
    private static PlayRecord ToPlayRecord(IPlayTally tally) =>
        new(tally.GamesPlayed, tally.Wins, tally.Losses);

    /// <summary>
    /// A set of server-side statistics keyed for lookup — a filter's history, a strip's per-level
    /// records, a spirit's per-aspect chips. The statistics page had spelled
    /// <c>new PlayRecord(x.GamesPlayed, x.Wins, x.Losses)</c> out per stat type, six times over.
    /// </summary>
    public static Dictionary<TKey, PlayRecord> PlayRecordsBy<T, TKey>(
        IEnumerable<T> tallies, Func<T, TKey> key)
        where T : IPlayTally
        where TKey : notnull =>
        tallies.ToDictionary(key, t => ToPlayRecord(t));

    /// <summary>
    /// A record at figure size — "3× · 67%", the rate only once something has finished. What an
    /// unplayed record says instead stays with the caller, which words it for its own space
    /// ("—" on a level tile, "never played" on the wider aspect face).
    /// </summary>
    public static string RecordLine(PlayRecord record) =>
        $"{record.Played}×{(record.Completed > 0 ? $" · {record.WinRate:F0}%" : "")}";

    /// <summary>
    /// Whether this record is the marked favourite of the set it belongs to — the starred board
    /// in the picker, the starred tile in the inline strip.
    /// <para>
    /// A lead of one play is no lead: with everything at 1× every entry would be marked, which
    /// marks nothing. The rule and that sentence were written out in both places that draw the
    /// mark, so the two were free to start disagreeing about what a favourite is.
    /// </para>
    /// </summary>
    /// <param name="mostPlayed">The highest play count in the set — see <see cref="Leader"/>.</param>
    public static bool IsFavourite(PlayRecord? record, int mostPlayed) =>
        record is { Played: > 0 } && record.Played == mostPlayed && mostPlayed > 1;

    /// <summary>The highest play count across a tally, or nought where nothing has been played —
    /// the figure <see cref="IsFavourite"/> measures against.</summary>
    public static int Leader(IReadOnlyDictionary<string, PlayRecord> records) =>
        records.Count == 0 ? 0 : records.Values.Max(r => r.Played);

    /// <summary>
    /// The results half of a record's tooltip: ", 2 won / 1 lost". Callers decide what an
    /// all-drafts record says instead — ", no results yet", or nothing at all. Five tooltips
    /// spelled this out and were one wording change from disagreeing.
    /// </summary>
    public static string WonLostSuffix(PlayRecord record) =>
        $", {record.Wins} won / {record.Losses} lost";

    /// <summary>
    /// How a game contributes to a tally: whether it finished, and how. Every record below is
    /// counted from a sequence of these, so "a draft counts as played but not as a result" is
    /// decided once in <see cref="Tally"/> rather than in each of a dozen group-bys.
    /// </summary>
    private readonly record struct Outcome(bool IsCompleted, bool? Win);

    private static PlayRecord Tally(IEnumerable<Outcome> outcomes)
    {
        var (played, wins, losses) = (0, 0, 0);
        foreach (var (completed, win) in outcomes)
        {
            played++;
            if (!completed) continue;
            if (win == true) wins++;
            else if (win == false) losses++;
        }
        return new PlayRecord(played, wins, losses);
    }

    /// <summary>Group a projection of seats or games by a key and tally each group.</summary>
    private static Dictionary<TKey, PlayRecord> TallyBy<TSource, TKey>(
        IEnumerable<TSource> source,
        Func<TSource, TKey> key,
        Func<TSource, Outcome> outcome) where TKey : notnull =>
        source.GroupBy(key).ToDictionary(g => g.Key, g => Tally(g.Select(outcome)));

    /// <summary>
    /// How a record reads on hover wherever one is offered as a small tally — "Anna: Wind —
    /// played 3×, 2 won / 1 lost". One helper so the chips, the variant rows and anything else
    /// that puts a <see cref="PlayRecord"/> on a control word it the same way.
    /// <paramref name="subjectName"/> names whose record it is when it isn't the reader's own;
    /// with <paramref name="hasHistory"/> false there is nothing to be never-played against, so
    /// the label stands alone.
    /// </summary>
    public static string RecordTooltip(
        string label, PlayRecord? record, string? subjectName, bool hasHistory = true)
    {
        if (!hasHistory) return label;
        var whose = subjectName is null ? "" : $"{subjectName}: ";
        if (record is not { Played: > 0 }) return $"{whose}never played {label}";
        var wins = record.Completed > 0 ? WonLostSuffix(record) : ", no results yet";
        return $"{whose}{label} — played {record.Played}×{wins}";
    }

    /// <summary>Overall record against one adversary, regardless of level.</summary>
    public sealed record AdversaryFacing(
        int Played,
        int Wins,
        int Losses,
        int? MostPlayedLevel,
        IReadOnlyDictionary<int, PlayRecord> Levels)
    {
        /// <summary>The record over every level at once — what a card's pill and a filter's
        /// ordering read, where the per-level split is the strip's business.</summary>
        public PlayRecord Overall => new(Played, Wins, Losses);
    }

    /// <summary>
    /// Games matching the current selection. Each picked adversary must be at the
    /// table (at its picked level when <paramref name="matchLevels"/> is true);
    /// a picked scenario must match when <paramref name="matchScenario"/> is true.
    /// Unset parts of the selection are "don't care" — with nothing selected this
    /// returns the full history.
    /// </summary>
    private static IReadOnlyList<SetupGameFact> Filter(
        IReadOnlyList<SetupGameFact> facts,
        IReadOnlyCollection<AdversaryPick> adversaries,
        string? scenarioId,
        bool matchLevels = true,
        bool matchScenario = true)
    {
        var picks = adversaries.Where(a => !string.IsNullOrEmpty(a.AdversaryId)).ToList();
        return facts
            .Where(g =>
                picks.All(p => g.Adversaries.Any(a =>
                    a.AdversaryId == p.AdversaryId
                    && (!matchLevels || p.Level is null || a.Level == p.Level)))
                && (!matchScenario || string.IsNullOrEmpty(scenarioId) || g.ScenarioId == scenarioId))
            .ToList();
    }

    public static MatchupRecord Record(IReadOnlyList<SetupGameFact> games)
    {
        var completed = games.Where(g => g.IsCompleted).ToList();
        var wins = completed.Count(g => g.Win == true);
        var last = games.MaxBy(g => g.StartedAt);
        var lastCompleted = completed.MaxBy(g => g.StartedAt);
        // Over the games that carry a score, as SetupProfiles.Build counts it: a finished game
        // recorded without one would otherwise make the best score null with results behind it,
        // and the matchup card then printed "Best score" with nothing after the words.
        var scored = completed.Where(g => g.Score is not null).ToList();
        return new MatchupRecord(
            games.Count,
            completed.Count,
            wins,
            completed.Count - wins,
            Percent.Of(wins, completed.Count),
            scored.Count > 0 ? scored.Max(g => g.Score) : null,
            last?.StartedAt,
            lastCompleted?.Win);
    }

    /// <summary>
    /// Per-spirit record inside a set of games. Counts every recorded seat at the
    /// table — friends' and local players' picks contribute too, matching how
    /// <see cref="BoardRecords"/> and the server-side statistics count. Pass
    /// <paramref name="seatFilter"/> to scope to one person.
    /// </summary>
    public static Dictionary<string, PlayRecord> SpiritRecords(
        IEnumerable<SetupGameFact> games,
        Func<SetupFactSeat, bool>? seatFilter = null)
    {
        return TallyBy(
            games.SelectMany(g => g.Seats
                .Where(s => seatFilter is null || seatFilter(s))
                .Select(s => (s.SpiritId, Outcome: new Outcome(g.IsCompleted, g.Win)))),
            x => x.SpiritId, x => x.Outcome);
    }

    /// <summary>
    /// The key standing for "the spirit as printed" in an aspect tally — the same empty-string
    /// convention <see cref="ScenarioRecords"/> uses for a game played without a scenario.
    /// </summary>
    public const string BaseAspectKey = "";

    /// <summary>
    /// Per-aspect record, for one spirit when <paramref name="spiritId"/> is set. An aspect
    /// rewrites the spirit enough that "played Lightning 11×" says little about how Wind went,
    /// so every surface that reports a spirit's record can report its variants too. Games played
    /// without an aspect land under <see cref="BaseAspectKey"/>, so the rows sum to the spirit's
    /// own total. Pass <paramref name="seatFilter"/> to scope to one person.
    /// <para>
    /// Aspect ids are unique across the catalogue, but <see cref="BaseAspectKey"/> is not: left
    /// unscoped it pools every spirit's aspect-less games into one bucket. Name the spirit, or
    /// use <see cref="AspectRecordsBySpirit"/>, whenever that row is going to be shown.
    /// </para>
    /// </summary>
    public static Dictionary<string, PlayRecord> AspectRecords(
        IEnumerable<SetupGameFact> games,
        string? spiritId = null,
        Func<SetupFactSeat, bool>? seatFilter = null)
    {
        return TallyBy(
            games.SelectMany(g => g.Seats
                .Where(s => seatFilter is null || seatFilter(s))
                .Where(s => spiritId is null || s.SpiritId == spiritId)
                .Select(s => (Key: s.AspectId ?? BaseAspectKey, Outcome: new Outcome(g.IsCompleted, g.Win)))),
            x => x.Key, x => x.Outcome);
    }

    /// <summary>
    /// <see cref="AspectRecords"/> for every spirit at once, keyed by spirit id then by aspect —
    /// for surfaces showing many spirits together, where each spirit's aspect-less games have to
    /// stay its own rather than pooling under a shared <see cref="BaseAspectKey"/>.
    /// </summary>
    public static Dictionary<string, Dictionary<string, PlayRecord>> AspectRecordsBySpirit(
        IEnumerable<SetupGameFact> games,
        Func<SetupFactSeat, bool>? seatFilter = null)
    {
        return games
            .SelectMany(g => g.Seats
                .Where(s => seatFilter is null || seatFilter(s))
                .Select(s => (s.SpiritId, Key: s.AspectId ?? BaseAspectKey, Outcome: new Outcome(g.IsCompleted, g.Win))))
            .GroupBy(x => x.SpiritId)
            .ToDictionary(
                spirit => spirit.Key,
                spirit => TallyBy(spirit, x => x.Key, x => x.Outcome));
    }

    /// <summary>
    /// No record at any level: a foe never faced, a spirit with no per-level history against the
    /// picked foe. One shared empty map rather than one per caller — <c>AdversaryLevelGrid</c>
    /// draws its record column as dashes from it, where a null would mean something else (no
    /// reader to have a record at all).
    /// </summary>
    public static readonly IReadOnlyDictionary<int, PlayRecord> NoLevels = new Dictionary<int, PlayRecord>();

    /// <summary>
    /// One spirit's history against the current selection, at three zoom levels:
    /// <see cref="Exact"/> = the selection as-is (levels + scenario),
    /// <see cref="Broad"/> = the picked adversaries at any level, any scenario,
    /// <see cref="ByLevel"/> = per-level records for a single picked adversary
    /// ("vs Russia: L1 1×, L2 2×...").
    /// </summary>
    public sealed record SpiritMatchup(
        PlayRecord Exact,
        PlayRecord Broad,
        IReadOnlyDictionary<int, PlayRecord> ByLevel);

    public static Dictionary<string, SpiritMatchup> SpiritMatchups(
        IReadOnlyList<SetupGameFact> facts,
        IReadOnlyCollection<AdversaryPick> adversaries,
        string? scenarioId,
        Func<SetupFactSeat, bool>? seatFilter = null)
    {
        var exactGames = Filter(facts, adversaries, scenarioId);
        var broadGames = Filter(facts, adversaries, scenarioId, matchLevels: false, matchScenario: false);
        var exact = SpiritRecords(exactGames, seatFilter);
        var broad = SpiritRecords(broadGames, seatFilter);

        var byLevel = new Dictionary<string, Dictionary<int, PlayRecord>>();
        var picks = adversaries.Where(a => !string.IsNullOrEmpty(a.AdversaryId)).ToList();
        if (picks.Count == 1)
        {
            var advId = picks[0].AdversaryId;
            var levelGroups = broadGames
                .Select(g => (Game: g, Adv: g.Adversaries.FirstOrDefault(a => a.AdversaryId == advId)))
                .Where(x => x.Adv is not null)
                .GroupBy(x => x.Adv!.Level);
            foreach (var group in levelGroups)
            {
                foreach (var (spiritId, rec) in SpiritRecords(group.Select(x => x.Game), seatFilter))
                {
                    if (!byLevel.TryGetValue(spiritId, out var levels))
                        byLevel[spiritId] = levels = new Dictionary<int, PlayRecord>();
                    levels[group.Key] = rec;
                }
            }
        }

        var none = new PlayRecord(0, 0, 0);
        return exact.Keys.Union(broad.Keys).Union(byLevel.Keys).ToDictionary(
            id => id,
            id => new SpiritMatchup(
                exact.GetValueOrDefault(id) ?? none,
                broad.GetValueOrDefault(id) ?? none,
                byLevel.GetValueOrDefault(id) ?? NoLevels));
    }

    /// <summary>
    /// Per-(spirit, adversary) record — the matchup grid's cells. One count per recorded seat
    /// per adversary at the table, the same per-seat rule <see cref="SpiritRecords"/> counts by;
    /// a game without an adversary contributes nothing, being no matchup at all. Pass
    /// <paramref name="seatFilter"/> to scope to one person's seats.
    /// </summary>
    public static Dictionary<(string SpiritId, string AdversaryId), PlayRecord> MatchupRecords(
        IEnumerable<SetupGameFact> facts,
        Func<SetupFactSeat, bool>? seatFilter = null)
    {
        return TallyBy(
            facts.SelectMany(g => g.Seats
                .Where(s => seatFilter is null || seatFilter(s))
                .SelectMany(s => g.Adversaries
                    .Select(a => (s.SpiritId, a.AdversaryId, Outcome: new Outcome(g.IsCompleted, g.Win))))),
            x => (x.SpiritId, x.AdversaryId), x => x.Outcome);
    }

    public static Dictionary<string, AdversaryFacing> AdversaryRecords(IReadOnlyList<SetupGameFact> facts)
    {
        return facts
            .SelectMany(g => g.Adversaries.Select(a => (a.AdversaryId, a.Level, Outcome: new Outcome(g.IsCompleted, g.Win))))
            .GroupBy(x => x.AdversaryId)
            .ToDictionary(
                grp => grp.Key,
                grp =>
                {
                    var overall = Tally(grp.Select(x => x.Outcome));
                    var levels = TallyBy(grp, x => x.Level, x => x.Outcome);
                    return new AdversaryFacing(
                        overall.Played,
                        overall.Wins,
                        overall.Losses,
                        levels.Count > 0 ? levels.MaxBy(kv => kv.Value.Played).Key : null,
                        levels);
                });
    }

    /// <summary>
    /// Per-board record — for one spirit when <paramref name="spiritId"/> is set
    /// (the spirit-on-board pairing). Counts every recorded seat at the table so
    /// friends' pairings contribute too; pass <paramref name="seatFilter"/> to
    /// scope to one person.
    /// </summary>
    public static Dictionary<string, PlayRecord> BoardRecords(
        IEnumerable<SetupGameFact> facts,
        string? spiritId = null,
        Func<SetupFactSeat, bool>? seatFilter = null)
    {
        return TallyBy(
            facts.SelectMany(g => g.Seats
                .Where(s => seatFilter is null || seatFilter(s))
                .Where(s => spiritId is null || s.SpiritId == spiritId)
                .Select(s => (s.BoardId, Outcome: new Outcome(g.IsCompleted, g.Win)))),
            x => x.BoardId, x => x.Outcome);
    }

    /// <summary>
    /// The key standing for "no scenario" in a scenario tally — the same empty-string convention
    /// <see cref="BaseAspectKey"/> follows for a spirit played as printed. Named so the picker
    /// that reads the row cannot spell the key differently from the tally that writes it.
    /// </summary>
    public const string NoScenarioKey = "";

    /// <summary>
    /// Per-scenario record across the whole history. Games played without a scenario ("standard
    /// game") sit under <see cref="NoScenarioKey"/>.
    /// </summary>
    public static Dictionary<string, PlayRecord> ScenarioRecords(IEnumerable<SetupGameFact> facts)
    {
        return TallyBy(facts, g => g.ScenarioId ?? NoScenarioKey, g => new Outcome(g.IsCompleted, g.Win));
    }

    /// <summary>
    /// Per-adversary record as a plain tally — for the filters and orderings that only want
    /// played / won / lost, without the per-level breakdown <see cref="AdversaryRecords"/> carries.
    /// </summary>
    public static Dictionary<string, PlayRecord> AdversaryPlayRecords(IReadOnlyList<SetupGameFact> facts) =>
        AdversaryPlayRecords(AdversaryRecords(facts));

    /// <summary>The same tally for a caller already holding the per-level records.</summary>
    public static Dictionary<string, PlayRecord> AdversaryPlayRecords(
        IReadOnlyDictionary<string, AdversaryFacing> facing) =>
        facing.ToDictionary(kv => kv.Key, kv => kv.Value.Overall);

    /// <summary>Record per island layout, keyed by setup id (hand-built one-offs group under
    /// their <c>custom-{n}p</c> placeholder ids).</summary>
    public static Dictionary<string, PlayRecord> LayoutRecords(IEnumerable<SetupGameFact> facts)
        => facts
            .Where(g => !string.IsNullOrEmpty(g.IslandSetupId))
            .GroupBy(g => g.IslandSetupId)
            .ToDictionary(g => g.Key, GameRecord);

    /// <summary>Record per saved layout, for the player's own shapes.</summary>
    public static Dictionary<Guid, PlayRecord> SavedLayoutRecords(IEnumerable<SetupGameFact> facts)
        => facts
            .Where(g => g.CustomLayoutId is not null)
            .GroupBy(g => g.CustomLayoutId!.Value)
            .ToDictionary(g => g.Key, GameRecord);

    /// <summary>
    /// Whole-game tally — for subjects the whole table shares, unlike the per-seat ones: an
    /// island, a scenario, the pool of one-off islands. The profile builder and the statistics
    /// page each had a copy of the three counts.
    /// </summary>
    public static PlayRecord GameRecord(IEnumerable<SetupGameFact> games) =>
        Tally(games.Select(g => new Outcome(g.IsCompleted, g.Win)));

    public static string Ago(DateTimeOffset when)
    {
        var days = (int)(DateTime.UtcNow.Date - when.UtcDateTime.Date).TotalDays;
        return days switch
        {
            <= 0 => "today",
            1 => "yesterday",
            < 14 => $"{days} days ago",
            < 60 => $"{days / 7} weeks ago",
            < 730 => $"{Math.Max(2, days / 30)} months ago",
            _ => $"{days / 365} years ago"
        };
    }
}
