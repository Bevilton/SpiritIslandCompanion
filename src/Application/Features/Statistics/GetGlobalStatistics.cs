using Application.Abstractions;
using Application.Data;
using Domain.Models.Game;
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

    public async Task<Result<GlobalStatisticsResponse>> Handle(
        GetGlobalStatisticsQuery request,
        CancellationToken cancellationToken)
    {
        var games = await db.Games
            .AsNoTracking()
            .Include(g => g.Players)
            .Include(g => g.PlayedAdversaries)
            .Include(g => g.Result)
            .ToListAsync(cancellationToken);

        var completed = games.Where(g => g.Result is not null).ToList();
        var wins = completed.Count(g => g.Result!.Win);

        var distinctPlayers = games
            .SelectMany(g => g.Players)
            .Select(p => (Guid?)(p.UserId?.Value) ?? p.PlayerId?.Value)
            .Where(id => id is not null)
            .Distinct()
            .Count();

        var topSpirits = games
            .SelectMany(g => g.Players.Select(p => new { p.SpiritId, g.Result }))
            .GroupBy(x => x.SpiritId.Value)
            .Select(grp =>
            {
                var done = grp.Where(x => x.Result is not null).ToList();
                var w = done.Count(x => x.Result!.Win);
                return new TopSpirit(
                    grp.Key,
                    grp.Count(),
                    done.Count,
                    done.Count > 0 ? (double)w / done.Count * 100 : 0);
            })
            .OrderByDescending(s => s.GamesPlayed)
            .Take(5)
            .ToList();

        var topAdversaries = games
            .SelectMany(g => g.PlayedAdversaries.Select(a => new { a.AdversaryId, g.Result }))
            .GroupBy(x => x.AdversaryId.Value)
            .Select(grp =>
            {
                var done = grp.Where(x => x.Result is not null).ToList();
                var w = done.Count(x => x.Result!.Win);
                return new TopAdversary(
                    grp.Key,
                    grp.Count(),
                    done.Count,
                    done.Count > 0 ? (double)w / done.Count * 100 : 0);
            })
            .OrderByDescending(s => s.GamesPlayed)
            .Take(5)
            .ToList();

        var difficultyBands = Bands
            .Select(b =>
            {
                bool InBand(Game g) => g.Difficulty.Value >= b.Min && g.Difficulty.Value <= b.Max;

                // Counted off `completed` rather than re-testing Result: the band's win rate is
                // only ever over finished games, and that list is already to hand.
                var done = completed.Where(InBand).ToList();
                return new DifficultyBand(
                    b.Label,
                    games.Count(InBand),
                    done.Count,
                    done.Count > 0 ? (double)done.Count(g => g.Result!.Win) / done.Count * 100 : 0);
            })
            .ToList();

        var response = new GlobalStatisticsResponse(
            TotalGames: games.Count,
            CompletedGames: completed.Count,
            TotalPlayers: distinctPlayers,
            WinRate: completed.Count > 0 ? (double)wins / completed.Count * 100 : 0,
            // Only completed games carry a duration; a draft has no result to have taken any time.
            TotalPlayTime: completed.Aggregate(TimeSpan.Zero, (sum, g) => sum + g.Result!.Duration),
            TopSpirits: topSpirits,
            TopAdversaries: topAdversaries,
            DifficultyBands: difficultyBands);

        return response;
    }
}
