using Domain.Models.Game;

namespace Application.Features.Statistics;

/// <summary>
/// How a set of games went: how many there were, and how the finished ones ended. The counts
/// every per-subject statistic is made of, taken in one place so that "completed" always means
/// wins plus losses and a rate is always over finished games only — a subject with nothing
/// finished has no record yet, not a losing one, which is why every tally that reports a rate
/// also reports the count it was taken over.
/// </summary>
internal readonly record struct Tally(int GamesPlayed, int Wins, int Losses)
{
    public int Completed => Wins + Losses;

    /// <summary>Wins as a percentage of finished games; zero while nothing is finished.</summary>
    public double WinRate => Completed > 0 ? (double)Wins / Completed * 100 : 0;

    /// <summary>The tally of a set of games by their outcomes, a draft's being null.</summary>
    public static Tally Of(IEnumerable<GameResult?> results) => Of(results.Select(r => r?.Outcome.Win));

    /// <summary>The same, for readers that project the outcome down to a flag in the database.</summary>
    public static Tally Of(IEnumerable<bool?> outcomes)
    {
        int played = 0, wins = 0, losses = 0;
        foreach (var win in outcomes)
        {
            played++;
            switch (win)
            {
                case true: wins++; break;
                case false: losses++; break;
            }
        }

        return new Tally(played, wins, losses);
    }
}
