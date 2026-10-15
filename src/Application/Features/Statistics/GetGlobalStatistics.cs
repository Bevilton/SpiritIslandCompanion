using Application.Abstractions;
using Application.Data;
using Domain.Models.Game;
using Domain.Models.Player;
using Domain.Models.Static;
using Domain.Models.User;
using Domain.Results;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Statistics;

public sealed record GetGlobalStatisticsQuery : IQuery<GlobalStatisticsResponse>;

public sealed record GlobalStatisticsResponse(
    int TotalGames,
    int CompletedGames,
    int TotalPlayers,
    double WinRate,
    TimeSpan TotalPlayTime,
    List<TopSpirit> TopSpirits,
    List<TopAdversary> TopAdversaries,
    List<DifficultyBand> DifficultyBands);

/// <summary>
/// <paramref name="Completed"/> is the denominator behind <paramref name="WinRate"/>, as on
/// <see cref="DifficultyBand"/>: a spirit can be the most-played one on the island with every one
/// of its games still a draft, and a rate of 0 over 0 finished games is not a losing record.
/// </summary>
public sealed record TopSpirit(string SpiritId, int GamesPlayed, int Completed, double WinRate);

/// <inheritdoc cref="TopSpirit"/>
public sealed record TopAdversary(string AdversaryId, int GamesPlayed, int Completed, double WinRate);

/// <summary>
/// How the community fares as the island gets harder. Banded rather than reported per exact
/// difficulty: the scale runs past 15, and single-difficulty buckets on a community-sized sample
/// swing between 0% and 100% on one or two games, which reads as noise rather than as a trend.
/// <paramref name="Completed"/> is the denominator behind <paramref name="WinRate"/>; games still
/// in progress are counted in <paramref name="Games"/> but cannot be won or lost yet.
/// </summary>
public sealed record DifficultyBand(string Label, int Games, int Completed, double WinRate);

internal sealed class GetGlobalStatisticsHandler(IAppDbContext db)
    : IQueryHandler<GetGlobalStatisticsQuery, GlobalStatisticsResponse>
{
    /// <summary>How many of the most-played spirits and adversaries the page shows.</summary>
    private const int TopCount = 5;

    /// <summary>
    /// The difficulty bands, held here rather than derived so the labels are the ones players
    /// actually use. Difficulty 0 is its own band because "no adversary at all" is a different
    /// kind of game, not merely an easier one; the top band is open-ended because the scale has
    /// no ceiling worth drawing.
    /// </summary>
    private static readonly (string Label, int Min, int Max)[] Bands =
    [
        ("0",    0,  0),
        ("1–3",  1,  3),
        ("4–6",  4,  6),
        ("7–9",  7,  9),
        ("10+", 10, int.MaxValue),
    ];

    /// <summary>
    /// A game as this page needs it — the few facts the tallies are made of, projected in the
    /// database. This is every game of every user, and the aggregates carry island geometry and
    /// notes that no community number is made from. <see cref="Win"/> and <see cref="Duration"/>
    /// are null for a draft, which has no outcome yet.
    /// </summary>
    private sealed record GameRow(
        Difficulty Difficulty,
        bool? Win,
        TimeSpan? Duration,
        List<SeatRow> Seats,
        List<AdversaryId> AdversaryIds);

    private sealed record SeatRow(SpiritId SpiritId, UserId? UserId, PlayerId? PlayerId);

    public async Task<Result<GlobalStatisticsResponse>> Handle(
        GetGlobalStatisticsQuery request,
        CancellationToken cancellationToken)
    {
        var games = await db.Games
            .AsNoTracking()
            .Select(g => new GameRow(
                g.Difficulty,
                (bool?)g.Result!.Outcome.Win,
                (TimeSpan?)g.Result!.Outcome.Duration,
                g.Players.Select(p => new SeatRow(p.SpiritId, p.UserId, p.PlayerId)).ToList(),
                g.PlayedAdversaries.Select(a => a.AdversaryId).ToList()))
            .ToListAsync(cancellationToken);

        var tally = Tally.Of(games.Select(g => g.Win));

        var distinctPlayers = games
            .SelectMany(g => g.Seats)
            .Select(s => s.UserId?.Value ?? s.PlayerId?.Value)
            .Where(id => id is not null)
            .Distinct()
            .Count();

        var difficultyBands = Bands
            .Select(b =>
            {
                var band = Tally.Of(games.Where(g => g.Difficulty.Value >= b.Min && g.Difficulty.Value <= b.Max).Select(g => g.Win));
                return new DifficultyBand(b.Label, band.GamesPlayed, band.Completed, band.WinRate);
            })
            .ToList();

        var response = new GlobalStatisticsResponse(
            TotalGames: tally.GamesPlayed,
            CompletedGames: tally.Completed,
            TotalPlayers: distinctPlayers,
            WinRate: tally.WinRate,
            // Only completed games carry a duration; a draft has no result to have taken any time.
            TotalPlayTime: TimeSpan.FromTicks(games.Sum(g => g.Duration?.Ticks ?? 0)),
            TopSpirits: MostPlayed(
                games.SelectMany(g => g.Seats.Select(s => (Id: s.SpiritId.Value, g.Win))),
                (id, t) => new TopSpirit(id, t.GamesPlayed, t.Completed, t.WinRate)),
            TopAdversaries: MostPlayed(
                games.SelectMany(g => g.AdversaryIds.Select(id => (Id: id.Value, g.Win))),
                (id, t) => new TopAdversary(id, t.GamesPlayed, t.Completed, t.WinRate)),
            DifficultyBands: difficultyBands);

        return response;
    }

    /// <summary>
    /// The <see cref="TopCount"/> most-played subjects among <paramref name="plays"/>, each with
    /// its record — one play being one game the subject was part of, with that game's outcome.
    /// Ties are broken by id, so which subject makes the cut does not depend on the order the
    /// rows happened to come back in.
    /// </summary>
    private static List<T> MostPlayed<T>(IEnumerable<(string Id, bool? Win)> plays, Func<string, Tally, T> make) =>
        plays
            .GroupBy(p => p.Id)
            .Select(grp => (grp.Key, Tally: Tally.Of(grp.Select(p => p.Win))))
            .OrderByDescending(x => x.Tally.GamesPlayed)
            .ThenBy(x => x.Key, StringComparer.Ordinal)
            .Take(TopCount)
            .Select(x => make(x.Key, x.Tally))
            .ToList();
}
