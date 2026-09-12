using Application.Features.Statistics;
using Domain.Models.Static;

namespace WebApp.Components.Shared.Stats;

/// <summary>
/// How much of an owned adversary catalogue has actually been faced, counted by level rather
/// than by foe: level 6 England is a different night than level 1, so a foe met once at L1 is
/// not a foe covered.
/// <para>
/// Two pages ask this about the same shelf and had each walked it themselves — the dashboard for
/// the fraction on its coverage bar, the statistics tab for the list its pills name — so "which
/// levels count as played" was decided twice for one number a reader sees in two places. One walk
/// now answers both.
/// </para>
/// </summary>
public static class LevelCoverage
{
    /// <param name="Missing">Every owned level never faced, foe by foe in catalogue order.</param>
    /// <param name="Played">How many owned levels have been faced.</param>
    /// <param name="Total">How many there are to face.</param>
    public sealed record Result(
        IReadOnlyList<MissingLevel> Missing, int Played, int Total);

    /// <param name="Level">The level itself — L0 is the foe at its gentlest, not "no adversary".</param>
    public sealed record MissingLevel(string AdversaryId, string AdversaryName, int Level);

    /// <summary>
    /// Measures <paramref name="ownedAdversaries"/> against what the record shows was faced.
    /// Levels the record holds for a foe that is <em>not</em> owned are ignored on both counts,
    /// as they must be: the tally is about the shelf, and a game played from somebody else's box
    /// can't make a level the reader doesn't have "covered" (the pages say so separately — see
    /// the outside-collection note on the statistics tab).
    /// </summary>
    public static Result Of(
        IEnumerable<AdversaryStats> stats,
        IReadOnlyList<Adversary> ownedAdversaries)
    {
        var faced = stats
            .SelectMany(a => a.Levels.Select(l => (a.AdversaryId, l.Level)))
            .ToHashSet();

        var missing = new List<MissingLevel>();
        var played = 0;
        var total = 0;

        foreach (var adversary in ownedAdversaries)
        {
            foreach (var mode in adversary.Modes.OrderBy(m => m.Level))
            {
                total++;
                if (faced.Contains((adversary.Id.Value, mode.Level)))
                {
                    played++;
                }
                else
                {
                    missing.Add(new MissingLevel(adversary.Id.Value, adversary.Name, mode.Level));
                }
            }
        }

        return new Result(missing, played, total);
    }
}
