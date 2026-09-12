namespace Application.Features.Statistics;

/// <summary>
/// The three counts every per-subject statistic reports: how often it was played, and how those
/// games ended. Spirits, adversaries and their levels, aspects, players, scenarios and boards all
/// carry them under the same names, which is what lets a reader project any of them into one
/// record shape without a lambda per type.
/// </summary>
public interface IPlayTally
{
    int GamesPlayed { get; }
    int Wins { get; }
    int Losses { get; }
}

/// <summary>
/// A tally that also reports its win rate, as the server computed it. The breakdown rows
/// (<see cref="AspectStats"/>, <see cref="AdversaryLevelStats"/>) deliberately do not — the rate
/// there is the reader's to derive.
/// </summary>
public interface IWinRateTally : IPlayTally
{
    double WinRate { get; }
}
