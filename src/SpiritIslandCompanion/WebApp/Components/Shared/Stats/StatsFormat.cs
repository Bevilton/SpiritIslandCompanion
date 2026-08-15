using Application.Features.Statistics;

namespace WebApp.Components.Shared.Stats;

/// <summary>
/// The wording the statistics surfaces share. The dashboard and the statistics page lead
/// with the same summary line and spell a duration the same way, so both live here rather
/// than as a copy per page that drifts the first time one of them is reworded.
/// </summary>
public static class StatsFormat
{
    /// <summary>A play time at a glance: "45m", "6h", "4d 3h".</summary>
    public static string Hours(TimeSpan span)
    {
        var totalHours = span.TotalHours;
        if (totalHours >= 24)
        {
            var days = (int)(totalHours / 24);
            var hrs = (int)(totalHours - days * 24);
            return hrs > 0 ? $"{days}d {hrs}h" : $"{days}d";
        }
        return totalHours >= 1 ? $"{(int)totalHours}h" : $"{(int)span.TotalMinutes}m";
    }

    /// <summary>
    /// "42 matches · 63% victories · 118h on the island" — the one-line page subtitle, the
    /// same idiom the games list leads with. Parts with nothing behind them are left out.
    /// </summary>
    public static string Summary(StatisticsResponse stats)
    {
        var parts = new List<string> { $"{stats.TotalGames} match{(stats.TotalGames == 1 ? "" : "es")}" };
        if (stats.Wins + stats.Losses > 0) parts.Add($"{stats.WinRate:F0}% victories");
        if (stats.TotalPlayTime > TimeSpan.Zero) parts.Add($"{Hours(stats.TotalPlayTime)} on the island");
        return string.Join(" · ", parts);
    }

    /// <summary>What sits under the games-recorded tile: the drafts still open, or that none are.</summary>
    public static string DraftsCaption(int inProgress) => inProgress > 0
        ? $"{inProgress} draft{(inProgress == 1 ? "" : "s")} waiting"
        : "all completed";
}
