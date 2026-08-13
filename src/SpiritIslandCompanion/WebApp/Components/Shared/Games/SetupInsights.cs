using Application.Features.Statistics;

namespace WebApp.Components.Shared.Games;

/// <summary>
/// In-memory matchup math for the game-setup screen. Every function operates on
/// the <see cref="SetupGameFact"/> history loaded once per page; the form simply
/// recomputes on each selection change (a personal history is at most a few
/// hundred rows, so there is no need to cache anything).
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
        public double WinRate => Completed > 0 ? (double)Wins / Completed * 100 : 0;
    }

    /// <summary>Overall record against one adversary, regardless of level.</summary>
    public sealed record AdversaryFacing(
        int Played,
        int Wins,
        int Losses,
        int? MostPlayedLevel,
        IReadOnlyDictionary<int, PlayRecord> Levels);

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
        return new MatchupRecord(
            games.Count,
            completed.Count,
            wins,
            completed.Count - wins,
            completed.Count > 0 ? (double)wins / completed.Count * 100 : 0,
            completed.Count > 0 ? completed.Max(g => g.Score) : null,
            last?.StartedAt,
            lastCompleted?.Win);
    }

    /// <summary>
    /// The seats attributed to the user: their own seat(s) when they sat at the
    /// table, otherwise every seat (games recorded for a table without the user).
    /// </summary>
    public static IEnumerable<SetupFactSeat> OwnSeats(SetupGameFact g)
        => g.Seats.Any(s => s.IsMine) ? g.Seats.Where(s => s.IsMine) : g.Seats;

    /// <summary>
    /// Per-spirit record inside a set of games. With <paramref name="seatFilter"/>
    /// set, only seats of that person count (e.g. a friend's seats); otherwise the
    /// own-seats heuristic applies.
    /// </summary>
    public static Dictionary<string, PlayRecord> SpiritRecords(
        IEnumerable<SetupGameFact> games,
        Func<SetupFactSeat, bool>? seatFilter = null)
    {
        return games
            .SelectMany(g => (seatFilter is null ? OwnSeats(g) : g.Seats.Where(seatFilter))
                .Select(s => (s.SpiritId, g.IsCompleted, g.Win)))
            .GroupBy(x => x.SpiritId)
            .ToDictionary(
                g => g.Key,
                g => new PlayRecord(
                    g.Count(),
                    g.Count(x => x.IsCompleted && x.Win == true),
                    g.Count(x => x.IsCompleted && x.Win == false)));
    }

    private static readonly IReadOnlyDictionary<int, PlayRecord> _noLevels = new Dictionary<int, PlayRecord>();

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
                byLevel.GetValueOrDefault(id) ?? _noLevels));
    }

    public static Dictionary<string, AdversaryFacing> AdversaryRecords(IReadOnlyList<SetupGameFact> facts)
    {
        return facts
            .SelectMany(g => g.Adversaries.Select(a => (a.AdversaryId, a.Level, Game: g)))
            .GroupBy(x => x.AdversaryId)
            .ToDictionary(
                grp => grp.Key,
                grp =>
                {
                    var completed = grp.Where(x => x.Game.IsCompleted).ToList();
                    var wins = completed.Count(x => x.Game.Win == true);
                    var levels = grp
                        .GroupBy(x => x.Level)
                        .ToDictionary(
                            lg => lg.Key,
                            lg => new PlayRecord(
                                lg.Count(),
                                lg.Count(x => x.Game.IsCompleted && x.Game.Win == true),
                                lg.Count(x => x.Game.IsCompleted && x.Game.Win == false)));
                    return new AdversaryFacing(
                        grp.Count(),
                        wins,
                        completed.Count - wins,
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
        return facts
            .SelectMany(g => g.Seats
                .Where(s => seatFilter is null || seatFilter(s))
                .Where(s => spiritId is null || s.SpiritId == spiritId)
                .Select(s => (s.BoardId, g.IsCompleted, g.Win)))
            .GroupBy(x => x.BoardId)
            .ToDictionary(
                g => g.Key,
                g => new PlayRecord(
                    g.Count(),
                    g.Count(x => x.IsCompleted && x.Win == true),
                    g.Count(x => x.IsCompleted && x.Win == false)));
    }

    /// <summary>
    /// Per-scenario record across the whole history. The empty-string key holds
    /// games played without a scenario ("standard game").
    /// </summary>
    public static Dictionary<string, PlayRecord> ScenarioRecords(IEnumerable<SetupGameFact> facts)
    {
        return facts
            .GroupBy(g => g.ScenarioId ?? "")
            .ToDictionary(
                g => g.Key,
                g => new PlayRecord(
                    g.Count(),
                    g.Count(x => x.IsCompleted && x.Win == true),
                    g.Count(x => x.IsCompleted && x.Win == false)));
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
        facing.ToDictionary(
            kv => kv.Key,
            kv => new PlayRecord(kv.Value.Played, kv.Value.Wins, kv.Value.Losses));

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

    /// <summary>Whole-game tally — for subjects the whole table shares, unlike the per-seat ones.</summary>
    private static PlayRecord GameRecord(IEnumerable<SetupGameFact> games)
    {
        var list = games.ToList();
        return new PlayRecord(
            list.Count,
            list.Count(g => g.IsCompleted && g.Win == true),
            list.Count(g => g.IsCompleted && g.Win == false));
    }

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
