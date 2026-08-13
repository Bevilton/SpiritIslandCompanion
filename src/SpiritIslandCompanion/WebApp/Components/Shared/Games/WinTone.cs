namespace WebApp.Components.Shared.Games;

/// <summary>
/// The single decision for how a win rate reads: green from 50% up, red below.
/// One home for the threshold so every record pill and level strip turns together.
/// </summary>
public static class WinTone
{
    /// <summary>Tone for a rate with completed games behind it.</summary>
    public static string ForRate(double winRate) =>
        winRate >= 50 ? "text-accent-700" : "text-red-600";

    /// <summary>Tone for a whole record: muted while nothing has finished, the rate's tone after.</summary>
    public static string ForRecord(SetupInsights.PlayRecord? record) =>
        record is not { Completed: > 0 } ? "text-ink-400" : ForRate(record.WinRate);
}
