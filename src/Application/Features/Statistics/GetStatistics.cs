using Application.Abstractions;
using Application.Data;
using Application.Features.Games;
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
    Guid? MinScoreGameId,
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
    List<int> CompletedDifficulties,
    List<TerrorLevelStats> TerrorDistribution);

public sealed record TerrorLevelStats(
    TerrorLevel TerrorLevel,
    int GamesPlayed,
    int Wins);

/// <summary>
/// One of the latest finished games. Always finished — see <c>GetRecentGames</c> — so the
/// outcome and the score are facts rather than possibilities.
/// </summary>
public sealed record RecentGameSummary(
    Guid Id,
    DateTimeOffset StartedAt,
    bool Win,
    int Score,
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
    int? BestScore,
    Dictionary<TerrorLevel, int> TerrorCounts,
    List<AspectStats> Aspects) : IWinRateTally;

/// <summary>
/// One variant of a spirit as it was actually played. An aspect rewrites enough of a
/// spirit that its record is its own; <see cref="AspectId"/> is null for the games
/// played with the spirit as printed, so the rows always add up to the spirit's total.
/// Like <see cref="AdversaryLevelStats"/> this is a breakdown of the row above it and
/// carries the tally only — the win rate is the reader's to derive.
/// </summary>
public sealed record AspectStats(
    string? AspectId,
    int GamesPlayed,
    int Wins,
    int Losses) : IPlayTally;

public sealed record AdversaryStats(
    string AdversaryId,
    int GamesPlayed,
    int Wins,
    int Losses,
    double WinRate,
    double AverageLevel,
    List<AdversaryLevelStats> Levels) : IWinRateTally;

public sealed record AdversaryLevelStats(int Level, int GamesPlayed, int Wins, int Losses) : IPlayTally;

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
    string? FavouriteSpirit) : IWinRateTally;

public sealed record ScenarioStats(
    string ScenarioId,
    int GamesPlayed,
    int Wins,
    int Losses,
    double WinRate,
    double AverageScore) : IWinRateTally;

public sealed record BoardStats(
    string BoardId,
    int GamesPlayed,
    int Wins,
    int Losses,
    double WinRate) : IWinRateTally;

/// <param name="GamesPlayed">Every game started that month, drafts included.</param>
/// <param name="Losses">
/// Finished games that were lost — carried rather than left to be derived, because
/// <c>GamesPlayed - Wins</c> counts the month's drafts as defeats.
/// </param>
public sealed record MonthlyStats(
    int Year,
    int Month,
    int GamesPlayed,
    int Wins,
    int Losses) : IPlayTally;

internal sealed class GetStatisticsHandler(IAppDbContext db, SeatNameResolver seatNames) : IQueryHandler<GetStatisticsQuery, StatisticsResponse>
{
    /// <summary>How many of the latest finished games the page shows as a strip.</summary>
    private const int RecentGameCount = 5;

    public async Task<Result<StatisticsResponse>> Handle(GetStatisticsQuery request, CancellationToken cancellationToken)
    {
        var games = await db.Games
            .InvolvingUser(new UserId(request.UserId))
            .ToListAsync(cancellationToken);

        var names = await seatNames.LoadAsync(games, cancellationToken);

        // PlayerStatistics is always computed across all games — it powers the
        // scope dropdown, so it shouldn't depend on the scope.
        var playerStatistics = GetPlayerStats(games, request.UserId, names);

        // Predicate that picks out the scoped player from a GamePlayer row.
        // Returns true for every row when no scope is set.
        Func<GamePlayer, bool> playerFilter = request switch
        {
            { ScopeUserId: { } u }   => p => p.UserId?.Value == u,
            { ScopePlayerId: { } l } => p => p.PlayerId?.Value == l,
            _                        => _ => true,
        };
        var isScoped = request.ScopeUserId is not null || request.ScopePlayerId is not null;

        // When scoped, restrict the entire dataset to games where the player
        // actually participated. When not scoped, everything is in play.
        var scopedGames = isScoped
            ? games.Where(g => g.Players.Any(playerFilter)).ToList()
            : games;

        var completedGames = scopedGames.Where(g => g.Result is not null).ToList();
        var tally = Tally.Of(scopedGames.Select(g => g.Result));

        var bestGame = completedGames.MaxBy(g => g.Result!.Score.Value);
        var worstGame = completedGames.MinBy(g => g.Result!.Score.Value);

        var response = new StatisticsResponse(
            TotalGames: tally.GamesPlayed,
            Wins: tally.Wins,
            Losses: tally.Losses,
            InProgress: tally.GamesPlayed - tally.Completed,
            WinRate: tally.WinRate,
            AverageDuration: completedGames.Count > 0
                ? TimeSpan.FromTicks((long)completedGames.Average(g => g.Result!.Outcome.Duration.Ticks))
                : TimeSpan.Zero,
            TotalPlayTime: TimeSpan.FromTicks(completedGames.Sum(g => g.Result!.Outcome.Duration.Ticks)),
            AverageDifficulty: scopedGames.Count > 0
                ? scopedGames.Average(g => g.Difficulty.Value)
                : 0,
            AverageScore: completedGames.Count > 0
                ? completedGames.Average(g => g.Result!.Score.Value)
                : 0,
            BestScore: bestGame?.Result!.Score.Value ?? 0,
            BestScoreGameId: bestGame?.Id.Value,
            MinScore: worstGame?.Result!.Score.Value,
            MinScoreGameId: worstGame?.Id.Value,
            AverageDahan: completedGames.Count > 0
                ? completedGames.Average(g => g.Result!.Outcome.Dahan.Value)
                : 0,
            AverageBlight: completedGames.Count > 0
                ? completedGames.Average(g => g.Result!.Outcome.Blight.Value)
                : 0,
            LongestStreak: ComputeLongestStreak(completedGames),
            SpiritStatistics: GetSpiritStats(scopedGames, playerFilter),
            AdversaryStatistics: GetAdversaryStats(scopedGames),
            PlayerStatistics: playerStatistics,
            ScenarioStatistics: GetScenarioStats(scopedGames),
            BoardStatistics: GetBoardStats(scopedGames, playerFilter),
            MonthlyStatistics: GetMonthlyStats(scopedGames),
            RecentGames: GetRecentGames(scopedGames),
            CompletedScores: completedGames.Select(g => g.Result!.Score.Value).ToList(),
            CompletedDifficulties: completedGames.Select(g => g.Difficulty.Value).ToList(),
            TerrorDistribution: GetTerrorDistribution(completedGames));

        return response;
    }

    private static List<SpiritStats> GetSpiritStats(List<Game> games, Func<GamePlayer, bool> playerFilter)
    {
        return games
            .SelectMany(g => g.Players.Where(playerFilter).Select(p => new { p.SpiritId, p.AspectId, g.Result }))
            .GroupBy(x => x.SpiritId.Value)
            .Select(g =>
            {
                var tally = Tally.Of(g.Select(x => x.Result));
                var completed = g.Where(x => x.Result is not null).ToList();
                var terrorCounts = completed
                    .GroupBy(x => x.Result!.Outcome.TerrorLevel)
                    .ToDictionary(tg => tg.Key, tg => tg.Count());
                // Null groups with null, so the spirit as printed is a row like any aspect
                // and the rows still sum to the spirit's own total.
                var aspects = g
                    .GroupBy(x => x.AspectId?.Value)
                    .Select(ag =>
                    {
                        var aspectTally = Tally.Of(ag.Select(x => x.Result));
                        return new AspectStats(ag.Key, aspectTally.GamesPlayed, aspectTally.Wins, aspectTally.Losses);
                    })
                    .OrderByDescending(a => a.GamesPlayed)
                    .ToList();
                return new SpiritStats(
                    g.Key,
                    tally.GamesPlayed,
                    tally.Wins,
                    tally.Losses,
                    tally.WinRate,
                    completed.Count > 0 ? completed.Average(x => x.Result!.Score.Value) : 0,
                    completed.Count > 0 ? completed.Max(x => x.Result!.Score.Value) : null,
                    terrorCounts,
                    aspects);
            })
            .OrderByDescending(s => s.GamesPlayed)
            .ToList();
    }

    private static List<TerrorLevelStats> GetTerrorDistribution(List<Game> completedGames)
    {
        var byLevel = completedGames
            .GroupBy(g => g.Result!.Outcome.TerrorLevel)
            .ToDictionary(g => g.Key, g => (Count: g.Count(), Wins: g.Count(x => x.Result!.Outcome.Win)));

        return Enum.GetValues<TerrorLevel>()
            .Select(t => byLevel.TryGetValue(t, out var v)
                ? new TerrorLevelStats(t, v.Count, v.Wins)
                : new TerrorLevelStats(t, 0, 0))
            .ToList();
    }

    private static List<AdversaryStats> GetAdversaryStats(List<Game> games)
    {
        return games
            .SelectMany(g => g.PlayedAdversaries.Select(a => new { a.AdversaryId, a.Level, g.Result }))
            .GroupBy(x => x.AdversaryId.Value)
            .Select(g =>
            {
                var tally = Tally.Of(g.Select(x => x.Result));
                var levels = g
                    .GroupBy(x => x.Level.Value)
                    .OrderBy(lg => lg.Key)
                    .Select(lg =>
                    {
                        var levelTally = Tally.Of(lg.Select(x => x.Result));
                        return new AdversaryLevelStats(lg.Key, levelTally.GamesPlayed, levelTally.Wins, levelTally.Losses);
                    })
                    .ToList();
                return new AdversaryStats(
                    g.Key,
                    tally.GamesPlayed,
                    tally.Wins,
                    tally.Losses,
                    tally.WinRate,
                    g.Average(x => x.Level.Value),
                    levels);
            })
            .OrderByDescending(s => s.GamesPlayed)
            .ToList();
    }

    private static List<PlayerStats> GetPlayerStats(List<Game> games, Guid currentUserId, SeatNames names)
    {
        return games
            .SelectMany(g => g.Players.Select(p => new
            {
                p.UserId,
                p.PlayerId,
                p.SpiritId,
                Game = g
            }))
            .GroupBy(x => new { UserId = x.UserId?.Value, PlayerId = x.PlayerId?.Value })
            .Select(g =>
            {
                // Per game, not per seat: one person taking three spirits played one evening,
                // and one evening has one outcome and one score. Counting the seats instead
                // would give a solo three-spirit win three wins, and the "games" on a person's
                // card would run ahead of the total on the same page.
                var played = g.Select(x => x.Game).Distinct().ToList();
                var tally = Tally.Of(played.Select(x => x.Result));
                var completed = played.Where(x => x.Result is not null).ToList();

                // Spirits stay per seat — all three of them really were played. Ties broken by
                // id, so the favourite and the top three read the same on every load: seats
                // come back from the database in no particular order.
                var spiritTallies = g
                    .GroupBy(x => x.SpiritId.Value)
                    .Select(sg => new { SpiritId = sg.Key, Count = sg.Count() })
                    .OrderByDescending(t => t.Count)
                    .ThenBy(t => t.SpiritId, StringComparer.Ordinal)
                    .ToList();

                var (name, kind) = names.Describe(g.Key.UserId, g.Key.PlayerId, currentUserId);

                return new PlayerStats(
                    g.Key.PlayerId,
                    g.Key.UserId,
                    name,
                    kind,
                    tally.GamesPlayed,
                    tally.Wins,
                    tally.Losses,
                    tally.WinRate,
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
            .Where(g => g.ScenarioId is not null)
            .GroupBy(g => g.ScenarioId!.Value)
            .Select(g =>
            {
                var tally = Tally.Of(g.Select(x => x.Result));
                var completed = g.Where(x => x.Result is not null).ToList();
                return new ScenarioStats(
                    g.Key,
                    tally.GamesPlayed,
                    tally.Wins,
                    tally.Losses,
                    tally.WinRate,
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
                var tally = Tally.Of(g.Select(x => x.Result));
                return new BoardStats(g.Key, tally.GamesPlayed, tally.Wins, tally.Losses, tally.WinRate);
            })
            .OrderByDescending(s => s.GamesPlayed)
            .ToList();
    }

    private static List<MonthlyStats> GetMonthlyStats(List<Game> games)
    {
        return games
            .GroupBy(g => new { g.StartedAt.Year, g.StartedAt.Month })
            .Select(g =>
            {
                var tally = Tally.Of(g.Select(x => x.Result));
                return new MonthlyStats(g.Key.Year, g.Key.Month, tally.GamesPlayed, tally.Wins, tally.Losses);
            })
            .OrderByDescending(s => s.Year)
            .ThenByDescending(s => s.Month)
            .ToList();
    }

    /// <summary>
    /// The latest games that actually have an outcome. Drafts are deliberately left out:
    /// with nothing to report they crowd finished games out of the strip, and the surfaces
    /// that care about unfinished games list them as work to do instead.
    /// </summary>
    private static List<RecentGameSummary> GetRecentGames(List<Game> games) =>
        games
            .Where(g => g.Result is not null)
            .OrderByDescending(g => g.StartedAt)
            .Take(RecentGameCount)
            .Select(g => new RecentGameSummary(
                g.Id.Value,
                g.StartedAt,
                g.Result!.Outcome.Win,
                g.Result.Score.Value,
                g.Difficulty.Value,
                g.Players.Count,
                g.PlayedAdversaries.Select(a => a.AdversaryId.Value).ToList(),
                g.ScenarioId?.Value,
                g.Players.Select(p => p.SpiritId.Value).ToList()))
            .ToList();

    private static int ComputeLongestStreak(List<Game> completedGames)
    {
        var best = 0;
        var current = 0;
        foreach (var g in completedGames.OrderBy(g => g.StartedAt))
        {
            if (g.Result!.Outcome.Win)
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
