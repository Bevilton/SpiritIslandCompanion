using Application.Abstractions;
using Application.Data;
using Domain.Results;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Statistics;

public sealed record GetGlobalStatisticsQuery : IQuery<GlobalStatisticsResponse>;

public sealed record GlobalStatisticsResponse(
    int TotalGames,
    int CompletedGames,
    int TotalPlayers,
    double WinRate,
    List<TopSpirit> TopSpirits,
    List<TopAdversary> TopAdversaries);

public sealed record TopSpirit(string SpiritId, int GamesPlayed, double WinRate);

public sealed record TopAdversary(string AdversaryId, int GamesPlayed, double WinRate);

internal sealed class GetGlobalStatisticsHandler(IAppDbContext db)
    : IQueryHandler<GetGlobalStatisticsQuery, GlobalStatisticsResponse>
{
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
                    done.Count > 0 ? (double)w / done.Count * 100 : 0);
            })
            .OrderByDescending(s => s.GamesPlayed)
            .Take(5)
            .ToList();

        var response = new GlobalStatisticsResponse(
            TotalGames: games.Count,
            CompletedGames: completed.Count,
            TotalPlayers: distinctPlayers,
            WinRate: completed.Count > 0 ? (double)wins / completed.Count * 100 : 0,
            TopSpirits: topSpirits,
            TopAdversaries: topAdversaries);

        return response;
    }
}
