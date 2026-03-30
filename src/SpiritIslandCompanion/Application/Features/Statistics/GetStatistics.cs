using Application.Abstractions;
using Application.Data;
using Domain.Models.Game;
using Domain.Models.User;
using Domain.Results;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Statistics;

public sealed record GetStatisticsQuery(Guid UserId) : IQuery<StatisticsResponse>;

public sealed record StatisticsResponse(
    int TotalGames,
    int Wins,
    int Losses,
    int InProgress,
    double WinRate,
    TimeSpan AverageDuration,
    double AverageDifficulty,
    double AverageScore,
    List<SpiritStats> SpiritStatistics,
    List<AdversaryStats> AdversaryStatistics,
    List<PlayerStats> PlayerStatistics,
    List<ScenarioStats> ScenarioStatistics,
    List<MonthlyStats> MonthlyStatistics);

public sealed record SpiritStats(
    string SpiritId,
    int GamesPlayed,
    int Wins,
    int Losses,
    double WinRate);

public sealed record AdversaryStats(
    string AdversaryId,
    int GamesPlayed,
    int Wins,
    int Losses,
    double WinRate,
    double AverageLevel);

public sealed record PlayerStats(
    Guid? PlayerId,
    Guid? UserId,
    int GamesPlayed,
    int Wins,
    int Losses,
    double WinRate,
    List<string> MostPlayedSpirits);

public sealed record ScenarioStats(
    string ScenarioId,
    int GamesPlayed,
    int Wins,
    int Losses,
    double WinRate);

public sealed record MonthlyStats(
    int Year,
    int Month,
    int GamesPlayed,
    int Wins);

internal sealed class GetStatisticsHandler(IAppDbContext db) : IQueryHandler<GetStatisticsQuery, StatisticsResponse>
{
    public async Task<Result<StatisticsResponse>> Handle(GetStatisticsQuery request, CancellationToken cancellationToken)
    {
        var games = await db.Games
            .AsNoTracking()
            .Include(g => g.Players)
            .Include(g => g.PlayedAdversaries)
            .Include(g => g.Result)
            .Include(g => g.Scenario)
            .Where(g => g.OwnerId.Value == request.UserId ||
                        g.Players.Any(p => p.UserId != null && p.UserId.Value == request.UserId))
            .ToListAsync(cancellationToken);

        var completedGames = games.Where(g => g.Result is not null).ToList();
        var wins = completedGames.Count(g => g.Result!.Win);
        var losses = completedGames.Count(g => !g.Result!.Win);
        var inProgress = games.Count(g => g.Result is null);

        var response = new StatisticsResponse(
            TotalGames: games.Count,
            Wins: wins,
            Losses: losses,
            InProgress: inProgress,
            WinRate: completedGames.Count > 0 ? (double)wins / completedGames.Count * 100 : 0,
            AverageDuration: completedGames.Count > 0
                ? TimeSpan.FromTicks((long)completedGames.Average(g => g.Result!.Duration.Ticks))
                : TimeSpan.Zero,
            AverageDifficulty: games.Count > 0
                ? games.Average(g => g.Difficulty.Value)
                : 0,
            AverageScore: completedGames.Count > 0
                ? completedGames.Average(g => g.Result!.Score.Value)
                : 0,
            SpiritStatistics: GetSpiritStats(games),
            AdversaryStatistics: GetAdversaryStats(games),
            PlayerStatistics: GetPlayerStats(games),
            ScenarioStatistics: GetScenarioStats(games),
            MonthlyStatistics: GetMonthlyStats(games));

        return response;
    }

    private static List<SpiritStats> GetSpiritStats(List<Game> games)
    {
        var spiritGames = games
            .SelectMany(g => g.Players.Select(p => new { p.SpiritId, g.Result }))
            .GroupBy(x => x.SpiritId.Value)
            .Select(g =>
            {
                var completed = g.Where(x => x.Result is not null).ToList();
                var w = completed.Count(x => x.Result!.Win);
                return new SpiritStats(
                    g.Key,
                    g.Count(),
                    w,
                    completed.Count - w,
                    completed.Count > 0 ? (double)w / completed.Count * 100 : 0);
            })
            .OrderByDescending(s => s.GamesPlayed)
            .ToList();

        return spiritGames;
    }

    private static List<AdversaryStats> GetAdversaryStats(List<Game> games)
    {
        var adversaryGames = games
            .SelectMany(g => g.PlayedAdversaries.Select(a => new { a.AdversaryId, a.Level, g.Result }))
            .GroupBy(x => x.AdversaryId.Value)
            .Select(g =>
            {
                var completed = g.Where(x => x.Result is not null).ToList();
                var w = completed.Count(x => x.Result!.Win);
                return new AdversaryStats(
                    g.Key,
                    g.Count(),
                    w,
                    completed.Count - w,
                    completed.Count > 0 ? (double)w / completed.Count * 100 : 0,
                    g.Average(x => x.Level.Value));
            })
            .OrderByDescending(s => s.GamesPlayed)
            .ToList();

        return adversaryGames;
    }

    private static List<PlayerStats> GetPlayerStats(List<Game> games)
    {
        var playerGames = games
            .SelectMany(g => g.Players.Select(p => new { p.UserId, p.PlayerId, p.SpiritId, g.Result }))
            .GroupBy(x => new { UserId = x.UserId?.Value, PlayerId = x.PlayerId?.Value })
            .Select(g =>
            {
                var completed = g.Where(x => x.Result is not null).ToList();
                var w = completed.Count(x => x.Result!.Win);
                var topSpirits = g
                    .GroupBy(x => x.SpiritId.Value)
                    .OrderByDescending(sg => sg.Count())
                    .Take(3)
                    .Select(sg => sg.Key)
                    .ToList();

                return new PlayerStats(
                    g.Key.PlayerId,
                    g.Key.UserId,
                    g.Count(),
                    w,
                    completed.Count - w,
                    completed.Count > 0 ? (double)w / completed.Count * 100 : 0,
                    topSpirits);
            })
            .OrderByDescending(s => s.GamesPlayed)
            .ToList();

        return playerGames;
    }

    private static List<ScenarioStats> GetScenarioStats(List<Game> games)
    {
        var scenarioGames = games
            .Where(g => g.Scenario is not null)
            .GroupBy(g => g.Scenario!.ScenarioId.Value)
            .Select(g =>
            {
                var completed = g.Where(x => x.Result is not null).ToList();
                var w = completed.Count(x => x.Result!.Win);
                return new ScenarioStats(
                    g.Key,
                    g.Count(),
                    w,
                    completed.Count - w,
                    completed.Count > 0 ? (double)w / completed.Count * 100 : 0);
            })
            .OrderByDescending(s => s.GamesPlayed)
            .ToList();

        return scenarioGames;
    }

    private static List<MonthlyStats> GetMonthlyStats(List<Game> games)
    {
        return games
            .GroupBy(g => new { g.StartedAt.Year, g.StartedAt.Month })
            .Select(g => new MonthlyStats(
                g.Key.Year,
                g.Key.Month,
                g.Count(),
                g.Count(x => x.Result is not null && x.Result.Win)))
            .OrderByDescending(s => s.Year)
            .ThenByDescending(s => s.Month)
            .ToList();
    }
}
