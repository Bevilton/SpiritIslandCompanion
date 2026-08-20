using Application.Features.Statistics;
using Domain.Models.Static.Data;

namespace WebApp.Components.Shared.Games;

/// <summary>Which catalogue entity a <see cref="SetupProfiles.Profile"/> describes.</summary>
public enum SetupSubjectKind
{
    Spirit,
    /// <summary>One variant of a spirit, matched by aspect id — profiled like the spirit itself.</summary>
    Aspect,
    Board,
    Adversary,
    Scenario,
    /// <summary>A published island layout, matched by its setup id.</summary>
    IslandLayout,
    /// <summary>A shape from the player's own library, matched by its <c>CustomLayoutId</c> as a string.</summary>
    SavedLayout
}

/// <summary>
/// Builds the "everything you've done with this thing" profile shown in the
/// spirit/board/adversary/scenario detail modals: a headline record plus
/// breakdowns of what it was paired with. Like <see cref="SetupInsights"/> this is
/// pure in-memory math over the <see cref="SetupGameFact"/> history the page has
/// already loaded, so opening a detail view costs no round-trip.
/// </summary>
public static class SetupProfiles
{
    /// <summary>One line of a breakdown, e.g. "Northeast (A) · 4× · 75%".</summary>
    public sealed record ProfileRow(
        string Label,
        string? Badge,
        string? ColorHex,
        SetupInsights.PlayRecord Record);

    /// <summary>
    /// One grouping of the subject's games — "Adversaries faced", "Boards used".
    /// <paramref name="Hidden"/> counts rows dropped past the display cap.
    /// </summary>
    public sealed record ProfileBreakdown(string Title, IReadOnlyList<ProfileRow> Rows, int Hidden);

    /// <summary>One game in the recent-games list; <see cref="Context"/> names the rest of the table.</summary>
    /// <param name="GameId">So a line in a record sheet can be opened as the game it reports.</param>
    public sealed record ProfileGame(
        Guid GameId,
        DateTimeOffset When,
        bool IsCompleted,
        bool? Win,
        int Difficulty,
        int? Score,
        string Context);

    public sealed record Profile(
        SetupSubjectKind Kind,
        SetupInsights.PlayRecord Record,
        int? BestScore,
        double? AverageScore,
        double? AverageDifficulty,
        int? HardestWon,
        DateTimeOffset? FirstPlayed,
        DateTimeOffset? LastPlayed,
        IReadOnlyList<ProfileGame> Recent,
        IReadOnlyList<ProfileBreakdown> Breakdowns);

    private const int MaxRows = 6;

    /// <summary>How many of the subject's most recent games the profile lists.</summary>
    private const int RecentGames = 5;

    /// <summary>A game that features the subject, with the seats that carry it.</summary>
    private sealed record Match(SetupGameFact Game, List<SetupFactSeat> Seats);

    /// <summary>
    /// Profiles one catalogue entity against the history. <paramref name="seatFilter"/>
    /// scopes seat-based numbers to one person (a friend sitting in the seat being
    /// edited); without it every seat at the table counts, so friends' and local
    /// players' games with the subject show up too — the "Who played it" breakdown
    /// then says whose they were.
    /// </summary>
    public static Profile Build(
        SetupSubjectKind kind,
        string id,
        IReadOnlyList<SetupGameFact> facts,
        Func<SetupFactSeat, bool>? seatFilter = null)
    {
        var matched = facts
            .Select(g => new Match(g, SubjectSeats(kind, id, g, seatFilter)))
            .Where(m => IsMatch(kind, id, m))
            .ToList();

        var games = matched.Select(m => m.Game).ToList();
        var completed = games.Where(g => g.IsCompleted).ToList();
        var scored = completed.Where(g => g.Score is not null).ToList();
        var won = completed.Where(g => g.Win == true).ToList();

        return new Profile(
            kind,
            RecordOf(games),
            scored.Count > 0 ? scored.Max(g => g.Score) : null,
            scored.Count > 0 ? scored.Average(g => g.Score!.Value) : null,
            games.Count > 0 ? games.Average(g => g.Difficulty) : null,
            won.Count > 0 ? won.Max(g => g.Difficulty) : null,
            games.Count > 0 ? games.Min(g => g.StartedAt) : null,
            games.Count > 0 ? games.Max(g => g.StartedAt) : null,
            matched
                .OrderByDescending(m => m.Game.StartedAt)
                .Take(RecentGames)
                .Select(m => new ProfileGame(
                    m.Game.GameId,
                    m.Game.StartedAt,
                    m.Game.IsCompleted,
                    m.Game.Win,
                    m.Game.Difficulty,
                    m.Game.Score,
                    Context(kind, id, m)))
                .ToList(),
            // A scoped profile skips the who-played rows: they'd hold the one
            // person the sheet is already labelled with.
            Breakdowns(kind, matched, includePlayers: seatFilter is null));
    }

    /// <summary>One person who has played the subject — an option for the sheet's owner toggle.</summary>
    public sealed record OwnerOption(
        string Key, string Label, string? ColorHex, int Played, Func<SetupFactSeat, bool> Filter);

    /// <summary>
    /// Who has played the subject, most-played first with the user's own seats leading.
    /// Only seat-bound subjects (spirit, board) have owners; other kinds return empty.
    /// </summary>
    public static IReadOnlyList<OwnerOption> Owners(
        SetupSubjectKind kind, string id, IReadOnlyList<SetupGameFact> facts)
    {
        if (kind is not (SetupSubjectKind.Spirit or SetupSubjectKind.Aspect or SetupSubjectKind.Board)) return [];
        return facts
            .SelectMany(g => SubjectSeats(kind, id, g, null))
            .GroupBy(OwnerKeyOf)
            .Select(grp =>
            {
                var owner = OwnerOf(grp.First());
                return new OwnerOption(grp.Key, owner.Label, owner.ColorHex, grp.Count(), FilterFor(grp.First()));
            })
            .OrderByDescending(o => o.Key == "me")
            .ThenByDescending(o => o.Played)
            .ThenBy(o => o.Label)
            .ToList();
    }

    private static string OwnerKeyOf(SetupFactSeat s) => s switch
    {
        { IsMine: true }       => "me",
        { UserId: { } user }   => $"u:{user}",
        { PlayerId: { } local } => $"p:{local}",
        _                      => "unassigned"
    };

    private static Func<SetupFactSeat, bool> FilterFor(SetupFactSeat seat) => seat switch
    {
        { IsMine: true }       => s => s.IsMine,
        { UserId: { } user }   => s => s.UserId == user,
        { PlayerId: { } local } => s => s.PlayerId == local,
        _                      => s => !s.IsMine && s.UserId is null && s.PlayerId is null
    };

    /// <summary>The seats carrying the subject — every counted seat when it isn't seat-bound.</summary>
    private static List<SetupFactSeat> SubjectSeats(
        SetupSubjectKind kind, string id, SetupGameFact game, Func<SetupFactSeat, bool>? seatFilter)
    {
        var counted = game.Seats.Where(s => seatFilter is null || seatFilter(s));
        return kind switch
        {
            SetupSubjectKind.Spirit => counted.Where(s => s.SpiritId == id).ToList(),
            SetupSubjectKind.Aspect => counted.Where(s => s.AspectId == id).ToList(),
            SetupSubjectKind.Board => counted.Where(s => s.BoardId == id).ToList(),
            _ => counted.ToList()
        };
    }

    private static bool IsMatch(SetupSubjectKind kind, string id, Match m) => kind switch
    {
        SetupSubjectKind.Spirit or SetupSubjectKind.Aspect or SetupSubjectKind.Board => m.Seats.Count > 0,
        SetupSubjectKind.Adversary => m.Game.Adversaries.Any(a => a.AdversaryId == id),
        SetupSubjectKind.IslandLayout => m.Game.IslandSetupId == id,
        SetupSubjectKind.SavedLayout => m.Game.CustomLayoutId?.ToString() == id,
        _ => m.Game.ScenarioId == id
    };

    private static SetupInsights.PlayRecord RecordOf(IEnumerable<SetupGameFact> games)
    {
        var list = games as ICollection<SetupGameFact> ?? games.ToList();
        return new SetupInsights.PlayRecord(
            list.Count,
            list.Count(g => g.IsCompleted && g.Win == true),
            list.Count(g => g.IsCompleted && g.Win == false));
    }

    private static IReadOnlyList<ProfileBreakdown> Breakdowns(
        SetupSubjectKind kind, List<Match> matched, bool includePlayers)
    {
        var players = includePlayers
            ? PlayerRowsIfShared(matched)
            : new ProfileBreakdown("Who played it", [], 0);
        ProfileBreakdown[] all = kind switch
        {
            SetupSubjectKind.Spirit =>
            [
                players,
                AspectRowsIfVaried(matched),
                AdversaryRows(matched),
                BoardRows(matched),
                ScenarioRows(matched),
                TableSizeRows(matched)
            ],
            // An aspect reads like the spirit it varies, minus the aspect breakdown that
            // would be a single row naming this sheet.
            SetupSubjectKind.Aspect =>
            [
                players,
                AdversaryRows(matched),
                BoardRows(matched),
                ScenarioRows(matched),
                TableSizeRows(matched)
            ],
            SetupSubjectKind.Board =>
            [
                players,
                SpiritRows(matched),
                AdversaryRows(matched),
                TableSizeRows(matched)
            ],
            // No level breakdown here — the adversary detail modal shows the record
            // per level inside its difficulty-by-level grid (see SetupInsights.AdversaryRecords).
            SetupSubjectKind.Adversary =>
            [
                SpiritRows(matched),
                ScenarioRows(matched),
                TableSizeRows(matched)
            ],
            // Layouts are the whole table's, not a seat's — no table-size row, because a
            // layout's board count already all but fixes it.
            SetupSubjectKind.IslandLayout or SetupSubjectKind.SavedLayout =>
            [
                SpiritRows(matched),
                AdversaryRows(matched),
                BoardRows(matched),
                ScenarioRows(matched)
            ],
            _ =>
            [
                AdversaryRows(matched),
                SpiritRows(matched),
                TableSizeRows(matched)
            ]
        };
        return all.Where(b => b.Rows.Count > 0).ToList();
    }

    /// <summary>Adversary keys, with an empty key standing for an unopposed game.</summary>
    private static IEnumerable<(string Key, SetupGameFact Game)> AdversaryKeys(List<Match> matched)
    {
        foreach (var m in matched)
        {
            if (m.Game.Adversaries.Count == 0)
            {
                yield return ("", m.Game);
                continue;
            }
            foreach (var adversary in m.Game.Adversaries)
                yield return (adversary.AdversaryId, m.Game);
        }
    }

    private static ProfileBreakdown AdversaryRows(List<Match> matched) =>
        Breakdown(
            "Adversaries faced",
            AdversaryKeys(matched),
            key => key.Length == 0
                ? new RowLabel("No adversary", null, null)
                : GameLookups.AdversaryFor(key) is { } adv ? new RowLabel(adv.Name, null, null) : null);

    private static ProfileBreakdown BoardRows(List<Match> matched) =>
        Breakdown(
            "Boards used",
            matched.SelectMany(m => m.Seats.Select(s => (Key: s.BoardId, Game: m.Game))),
            key =>
            {
                if (GameLookups.BoardFor(key) is not { } board) return null;
                var detail = BoardDetails.For(board.Id);
                return new RowLabel(detail?.ThematicName ?? board.Name, GameLookups.BoardLetter(board), detail?.ColorHex);
            },
            max: 8);

    /// <summary>
    /// "Aspects played" on a spirit's sheet. The spirit as printed is a row of its own, so the
    /// counts add up to the headline above them rather than accounting only for the variants —
    /// and, as with "Who played it", a single row is left out: it would only restate that
    /// headline under a title claiming to split it.
    /// </summary>
    private static ProfileBreakdown AspectRowsIfVaried(List<Match> matched)
    {
        var rows = Breakdown(
            "Aspects played",
            matched.SelectMany(m => m.Seats.Select(s => (Key: s.AspectId ?? SetupInsights.BaseAspectKey, Game: m.Game))),
            key => key.Length == 0
                ? new RowLabel("No aspect", null, null)
                : GameLookups.AspectFor(key) is { } aspect ? new RowLabel(aspect.Name, null, null) : null);
        return rows.Rows.Count > 1 ? rows : rows with { Rows = [] };
    }

    private static ProfileBreakdown SpiritRows(List<Match> matched) =>
        Breakdown(
            "Spirits played",
            matched.SelectMany(m => m.Seats.Select(s => (Key: s.SpiritId, Game: m.Game))),
            key => GameLookups.SpiritFor(key) is { } spirit
                ? new RowLabel(spirit.Name, null, SpiritDetails.For(spirit.Id)?.ColorHex)
                : null);

    /// <summary>
    /// Groups the subject's seats by who sat in them. Equality is on the resolved
    /// label, so "You" is one row and two unnamed seats stay together. Dot colours
    /// follow the assignee badge: accent for you, sky for friends, ink for locals.
    /// </summary>
    private sealed record SeatOwner(string Label, string? ColorHex);

    private static SeatOwner OwnerOf(SetupFactSeat s) => s switch
    {
        { IsMine: true }       => new SeatOwner("You", "#059669"),
        { UserId: not null }   => new SeatOwner(s.PlayerName ?? "Friend", "#0284c7"),
        { PlayerId: not null } => new SeatOwner(s.PlayerName ?? "Player", "#a8a29e"),
        _                      => new SeatOwner("Unassigned", null)
    };

    /// <summary>
    /// "Who played it" — only when the answer isn't just "you": a lone "You" row
    /// would restate every other number on the sheet.
    /// </summary>
    private static ProfileBreakdown PlayerRowsIfShared(List<Match> matched)
    {
        var rows = Breakdown(
            "Who played it",
            matched.SelectMany(m => m.Seats.Select(s => (Key: OwnerOf(s), Game: m.Game))),
            key => new RowLabel(key.Label, null, key.ColorHex));
        var informative = rows.Rows.Count > 1 || rows.Rows.Any(r => r.Label != "You");
        return informative ? rows : rows with { Rows = [] };
    }

    private static ProfileBreakdown ScenarioRows(List<Match> matched) =>
        Breakdown(
            "Scenarios",
            matched.Select(m => (Key: m.Game.ScenarioId ?? "", Game: m.Game)),
            key => key.Length == 0
                ? new RowLabel("No scenario", null, null)
                : GameLookups.ScenarioFor(key) is { } scen ? new RowLabel(scen.Name, null, null) : null);

    private static ProfileBreakdown TableSizeRows(List<Match> matched) =>
        Breakdown(
            "Table size",
            matched.Select(m => (Key: m.Game.Seats.Count, Game: m.Game)),
            size => new RowLabel(size == 1 ? "Solo" : $"{size} players", null, null),
            sortByKey: true);

    private sealed record RowLabel(string Text, string? Badge, string? ColorHex);

    /// <summary>
    /// Groups games by a key, turning each group into a row. Rows are ordered by
    /// play count — or by the key itself when <paramref name="sortByKey"/>, for
    /// naturally sequential keys like table size — and capped at <paramref name="max"/>.
    /// </summary>
    private static ProfileBreakdown Breakdown<TKey>(
        string title,
        IEnumerable<(TKey Key, SetupGameFact Game)> pairs,
        Func<TKey, RowLabel?> describe,
        int max = MaxRows,
        bool sortByKey = false)
        where TKey : notnull
    {
        var groups = pairs
            .DistinctBy(p => (p.Key, p.Game.GameId))
            .GroupBy(p => p.Key)
            .Select(g => (g.Key, Label: describe(g.Key), Record: RecordOf(g.Select(x => x.Game))))
            .Where(x => x.Label is not null)
            .ToList();

        var rows = (sortByKey
                ? groups.OrderBy(x => x.Key)
                : groups.OrderByDescending(x => x.Record.Played).ThenBy(x => x.Label!.Text))
            .Select(x => new ProfileRow(x.Label!.Text, x.Label.Badge, x.Label.ColorHex, x.Record))
            .ToList();

        var shown = rows.Take(max).ToList();
        return new ProfileBreakdown(title, shown, rows.Count - shown.Count);
    }

    /// <summary>
    /// Names the rest of the table for a recent-games line — everything except the
    /// subject itself ("board A · vs England L3 · Blitz" on a spirit profile).
    /// </summary>
    private static string Context(SetupSubjectKind kind, string id, Match m)
    {
        var parts = new List<string>();

        if (kind == SetupSubjectKind.Adversary)
        {
            var levels = m.Game.Adversaries.Where(a => a.AdversaryId == id).Select(a => $"L{a.Level}");
            parts.Add(string.Join(" + ", levels));
        }

        // A spirit sheet (and an aspect's, which is one spirit's) names the boards it sat on;
        // every other sheet names the spirits at the table.
        if (kind is not (SetupSubjectKind.Spirit or SetupSubjectKind.Aspect))
        {
            var spirits = m.Seats
                .Select(s => GameLookups.SpiritFor(s.SpiritId)?.Name)
                .OfType<string>()
                .Distinct()
                .ToList();
            if (spirits.Count > 0) parts.Add(Summarise(spirits, 2));
        }
        else
        {
            var boards = m.Seats
                .Select(s => GameLookups.BoardFor(s.BoardId))
                .OfType<Domain.Models.Static.Board>()
                .Select(GameLookups.BoardLetter)
                .Distinct()
                .ToList();
            if (boards.Count > 0) parts.Add($"board {string.Join(" + ", boards)}");
        }

        if (kind != SetupSubjectKind.Adversary)
        {
            var foes = m.Game.Adversaries
                .Select(a => GameLookups.AdversaryFor(a.AdversaryId) is { } adv ? $"{adv.Name} L{a.Level}" : null)
                .OfType<string>()
                .ToList();
            parts.Add(foes.Count > 0 ? $"vs {Summarise(foes, 2)}" : "no adversary");
        }

        if (kind != SetupSubjectKind.Scenario && GameLookups.ScenarioFor(m.Game.ScenarioId) is { } scenario)
            parts.Add(scenario.Name);

        return string.Join(" · ", parts.Where(p => p.Length > 0));
    }

    private static string Summarise(IReadOnlyList<string> names, int max) =>
        names.Count <= max
            ? string.Join(" + ", names)
            : $"{string.Join(" + ", names.Take(max))} +{names.Count - max}";
}
