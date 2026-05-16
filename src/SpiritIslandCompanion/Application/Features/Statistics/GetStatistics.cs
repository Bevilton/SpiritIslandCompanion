using Application.Abstractions;
using Application.Data;
using Domain.Models.Game;
using Domain.Models.User;
using Domain.Results;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Statistics;

/// <summary>
/// Returns the statistics for the requesting user's recorded games.
/// When <see cref="ScopeUserId"/> or <see cref="ScopePlayerId"/> is supplied,
/// the aggregations are filtered to games where that specific player
/// participated, and spirit / board stats only reflect that player's picks.
/// </summary>
public sealed record GetStatisticsQuery(
    Guid UserId,
    Guid? ScopeUserId = null,
    Guid? ScopePlayerId = null) : IQuery<StatisticsResponse>;

public sealed record StatisticsResponse(
    int TotalGames,
    int Wins,
    int Losses,
    int InProgress,
    double WinRate,
    TimeSpan AverageDuration,
    TimeSpan TotalPlayTime,
    double AverageDifficulty,
    double AverageScore,
    int BestScore,
    Guid? BestScoreGameId,
    int? MinScore,
    double AverageDahan,
    double AverageBlight,
    int LongestStreak,
    List<SpiritStats> SpiritStatistics,
    List<AdversaryStats> AdversaryStatistics,
    List<PlayerStats> PlayerStatistics,
    List<ScenarioStats> ScenarioStatistics,
    List<BoardStats> BoardStatistics,
    List<MonthlyStats> MonthlyStatistics,
    List<RecentGameSummary> RecentGames,
    List<int> CompletedScores,
    List<int> CompletedDifficulties);

public sealed record RecentGameSummary(
    Guid Id,
    DateTimeOffset StartedAt,
    bool IsCompleted,
    bool? Win,
    int? Score,
    int Difficulty,
    int PlayerCount,
    List<string> AdversaryIds,
    string? ScenarioId,
    List<string> SpiritIds);

public sealed record SpiritStats(
    string SpiritId,
    int GamesPlayed,
    int Wins,
    int Losses,
    double WinRate,
    double AverageScore,
    int? BestScore);

public sealed record AdversaryStats(
    string AdversaryId,
    int GamesPlayed,
    int Wins,
    int Losses,
    double WinRate,
    double AverageLevel,
    List<AdversaryLevelStats> Levels);

public sealed record AdversaryLevelStats(int Level, int GamesPlayed, int Wins);

public enum PlayerKind { Me, Friend, Local }

public sealed record PlayerStats(
    Guid? PlayerId,
    Guid? UserId,
    string Name,
    PlayerKind Kind,
    int GamesPlayed,
    int Wins,
    int Losses,
    double WinRate,
    double AverageScore,
    int? BestScore,
    List<string> MostPlayedSpirits,
    string? FavouriteSpirit);

public sealed record ScenarioStats(
    string ScenarioId,
    int GamesPlayed,
    int Wins,
    int Losses,
    double WinRate,
    double AverageScore);

public sealed record BoardStats(
    string BoardId,
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

        // Resolve display names for every user / player referenced in any game.
        // We pass the value-object types directly so EF Core's configured
        // HasConversion translates Contains into a SQL IN clause.
        var userIds = games.SelectMany(g => g.Players)
            .Where(p => p.UserId is not null)
            .Select(p => p.UserId!)
            .Concat(games.Select(g => g.OwnerId))
            .Distinct()
            .ToList();
        var userLookup = (await db.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToListAsync(cancellationToken))
            .ToDictionary(u => u.Id.Value, u => u.Nickname.Value);

        var playerIds = games.SelectMany(g => g.Players)
            .Where(p => p.PlayerId is not null)
            .Select(p => p.PlayerId!)
            .Distinct()
            .ToList();
        var playerLookup = (await db.Players
            .AsNoTracking()
            .Where(p => playerIds.Contains(p.Id))
            .ToListAsync(cancellationToken))
            .ToDictionary(p => p.Id.Value, p => p.Name.Value);

        // PlayerStatistics is always computed across all games — it powers the
        // scope dropdown, so it shouldn't depend on the scope.
        var playerStatistics = GetPlayerStats(games, request.UserId, userLookup, playerLookup);

        // Predicate that picks out the scoped player from a GamePlayer row.
        // Returns true for every row when no scope is set.
        Func<GamePlayer, bool> playerFilter = request switch
        {
            { ScopeUserId: { } u }   => p => p.UserId is not null && p.UserId.Value == u,
            { ScopePlayerId: { } l } => p => p.PlayerId is not null && p.PlayerId.Value == l,
            _                        => _ => true,
        };
        var isScoped = request.ScopeUserId is not null || request.ScopePlayerId is not null;

        // When scoped, restrict the entire dataset to games where the player
        // actually participated. When not scoped, everything is in play.
        var scopedGames = isScoped
            ? games.Where(g => g.Players.Any(playerFilter)).ToList()
            : games;

        var completedGames = scopedGames.Where(g => g.Result is not null).ToList();
        var wins = completedGames.Count(g => g.Result!.Win);
        var losses = completedGames.Count(g => !g.Result!.Win);
        var inProgress = scopedGames.Count(g => g.Result is null);

        var bestGame = completedGames.MaxBy(g => g.Result!.Score.Value);
        var minScore = completedGames.Count > 0
            ? completedGames.Min(g => g.Result!.Score.Value)
            : (int?)null;
        var totalPlayTime = completedGames.Count > 0
            ? TimeSpan.FromTicks(completedGames.Sum(g => g.Result!.Duration.Ticks))
            : TimeSpan.Zero;
        var longestStreak = ComputeLongestStreak(completedGames);

        var response = new StatisticsResponse(
            TotalGames: scopedGames.Count,
            Wins: wins,
            Losses: losses,
            InProgress: inProgress,
            WinRate: completedGames.Count > 0 ? (double)wins / completedGames.Count * 100 : 0,
            AverageDuration: completedGames.Count > 0
                ? TimeSpan.FromTicks((long)completedGames.Average(g => g.Result!.Duration.Ticks))
                : TimeSpan.Zero,
            TotalPlayTime: totalPlayTime,
            AverageDifficulty: scopedGames.Count > 0
                ? scopedGames.Average(g => g.Difficulty.Value)
                : 0,
            AverageScore: completedGames.Count > 0
                ? completedGames.Average(g => g.Result!.Score.Value)
                : 0,
            BestScore: bestGame?.Result!.Score.Value ?? 0,
            BestScoreGameId: bestGame?.Id.Value,
            MinScore: minScore,
            AverageDahan: completedGames.Count > 0
                ? completedGames.Average(g => g.Result!.Dahan.Value)
                : 0,
            AverageBlight: completedGames.Count > 0
                ? completedGames.Average(g => g.Result!.Blight.Value)
                : 0,
            LongestStreak: longestStreak,
            SpiritStatistics: GetSpiritStats(scopedGames, playerFilter),
            AdversaryStatistics: GetAdversaryStats(scopedGames),
            PlayerStatistics: playerStatistics,
            ScenarioStatistics: GetScenarioStats(scopedGames),
            BoardStatistics: GetBoardStats(scopedGames, playerFilter),
            MonthlyStatistics: GetMonthlyStats(scopedGames),
            RecentGames: GetRecentGames(scopedGames),
            CompletedScores: completedGames.Select(g => g.Result!.Score.Value).ToList(),
            CompletedDifficulties: completedGames.Select(g => g.Difficulty.Value).ToList());

        return response;
    }

    private static List<SpiritStats> GetSpiritStats(List<Game> games, Func<GamePlayer, bool> playerFilter)
    {
        return games
            .SelectMany(g => g.Players.Where(playerFilter).Select(p => new { p.SpiritId, g.Result }))
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
                    completed.Count > 0 ? (double)w / completed.Count * 100 : 0,
                    completed.Count > 0 ? completed.Average(x => x.Result!.Score.Value) : 0,
                    completed.Count > 0 ? completed.Max(x => x.Result!.Score.Value) : null);
            })
            .OrderByDescending(s => s.GamesPlayed)
            .ToList();
    }

    private static List<AdversaryStats> GetAdversaryStats(List<Game> games)
    {
        return games
            .SelectMany(g => g.PlayedAdversaries.Select(a => new { a.AdversaryId, a.Level, g.Result }))
            .GroupBy(x => x.AdversaryId.Value)
            .Select(g =>
            {
                var completed = g.Where(x => x.Result is not null).ToList();
                var w = completed.Count(x => x.Result!.Win);
                var levels = g
                    .GroupBy(x => x.Level.Value)
                    .OrderBy(lg => lg.Key)
                    .Select(lg =>
                    {
                        var levelCompleted = lg.Where(x => x.Result is not null).ToList();
                        return new AdversaryLevelStats(
                            lg.Key,
                            lg.Count(),
                            levelCompleted.Count(x => x.Result!.Win));
                    })
                    .ToList();
                return new AdversaryStats(
                    g.Key,
                    g.Count(),
                    w,
                    completed.Count - w,
                    completed.Count > 0 ? (double)w / completed.Count * 100 : 0,
                    g.Average(x => x.Level.Value),
                    levels);
            })
            .OrderByDescending(s => s.GamesPlayed)
            .ToList();
    }

    private static List<PlayerStats> GetPlayerStats(
        List<Game> games,
        Guid currentUserId,
        Dictionary<Guid, string> userLookup,
        Dictionary<Guid, string> playerLookup)
    {
        return games
            .SelectMany(g => g.Players.Select(p => new
            {
                p.UserId,
                p.PlayerId,
                p.SpiritId,
                g.Result
            }))
            .GroupBy(x => new { UserId = x.UserId?.Value, PlayerId = x.PlayerId?.Value })
            .Select(g =>
            {
                var completed = g.Where(x => x.Result is not null).ToList();
                var w = completed.Count(x => x.Result!.Win);
                var spiritTallies = g
                    .GroupBy(x => x.SpiritId.Value)
                    .Select(sg => new { SpiritId = sg.Key, Count = sg.Count() })
                    .OrderByDescending(t => t.Count)
                    .ToList();

                string name;
                PlayerKind kind;
                if (g.Key.UserId is { } uid)
                {
                    kind = uid == currentUserId ? PlayerKind.Me : PlayerKind.Friend;
                    name = kind == PlayerKind.Me
                        ? "You"
                        : userLookup.GetValueOrDefault(uid, "Unknown");
                }
                else if (g.Key.PlayerId is { } pid)
                {
                    kind = PlayerKind.Local;
                    name = playerLookup.GetValueOrDefault(pid, "Unknown");
                }
                else
                {
                    kind = PlayerKind.Local;
                    name = "Unassigned";
                }

                return new PlayerStats(
                    g.Key.PlayerId,
                    g.Key.UserId,
                    name,
                    kind,
                    g.Count(),
                    w,
                    completed.Count - w,
                    completed.Count > 0 ? (double)w / completed.Count * 100 : 0,
                    completed.Count > 0 ? completed.Average(x => x.Result!.Score.Value) : 0,
                    completed.Count > 0 ? completed.Max(x => x.Result!.Score.Value) : null,
                    spiritTallies.Take(3).Select(t => t.SpiritId).ToList(),
                    spiritTallies.FirstOrDefault()?.SpiritId);
            })
            .OrderByDescending(s => s.GamesPlayed)
            .ToList();
    }

    private static List<ScenarioStats> GetScenarioStats(List<Game> games)
    {
        return games
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
                    completed.Count > 0 ? (double)w / completed.Count * 100 : 0,
                    completed.Count > 0 ? completed.Average(x => x.Result!.Score.Value) : 0);
            })
            .OrderByDescending(s => s.GamesPlayed)
            .ToList();
    }

    private static List<BoardStats> GetBoardStats(List<Game> games, Func<GamePlayer, bool> playerFilter)
    {
        return games
            .SelectMany(g => g.Players.Where(playerFilter).Select(p => new { p.StartingBoard, g.Result }))
            .GroupBy(x => x.StartingBoard.Value)
            .Select(g =>
            {
                var completed = g.Where(x => x.Result is not null).ToList();
                var w = completed.Count(x => x.Result!.Win);
                return new BoardStats(
                    g.Key,
                    g.Count(),
                    w,
                    completed.Count - w,
                    completed.Count > 0 ? (double)w / completed.Count * 100 : 0);
            })
            .OrderByDescending(s => s.GamesPlayed)
            .ToList();
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

    private static List<RecentGameSummary> GetRecentGames(List<Game> games) =>
        games
            .OrderByDescending(g => g.StartedAt)
            .Take(5)
            .Select(g => new RecentGameSummary(
                g.Id.Value,
                g.StartedAt,
                g.Result is not null,
                g.Result?.Win,
                g.Result?.Score.Value,
                g.Difficulty.Value,
                g.Players.Count,
                g.PlayedAdversaries.Select(a => a.AdversaryId.Value).ToList(),
                g.Scenario?.ScenarioId.Value,
                g.Players.Select(p => p.SpiritId.Value).ToList()))
            .ToList();

    private static int ComputeLongestStreak(List<Game> completedGames)
    {
        var best = 0;
        var current = 0;
        foreach (var g in completedGames.OrderBy(g => g.StartedAt))
        {
            if (g.Result!.Win)
            {
                current++;
                if (current > best) best = current;
            }
            else
            {
                current = 0;
            }
        }
        return best;
    }
}
