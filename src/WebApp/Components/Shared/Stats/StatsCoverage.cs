using Application.Features.Statistics;
using Domain.Models.Static;

namespace WebApp.Components.Shared.Stats;

/// <summary>
/// What an owned catalogue is missing, in the shapes the coverage panels' pills are built from,
/// and the note on what a coverage tally deliberately leaves out. The statistics page held these
/// as private records and methods among its tab fragments; they are arithmetic over the shelf
/// and the record, not presentation, and belong beside <see cref="LevelCoverage"/>, which walks
/// the adversary shelf the same way.
/// </summary>
public static class StatsCoverage
{
    /// <summary>One owned aspect never played.</summary>
    public sealed record MissingAspect(string SpiritId, string SpiritName, string AspectName);

    /// <summary>A spirit's missing aspects, for one pill per spirit.</summary>
    public sealed record MissingAspectGroup(string SpiritId, string SpiritName, IReadOnlyList<string> AspectNames);

    /// <summary>A foe's missing levels, for one pill per foe.</summary>
    public sealed record MissingAdversaryGroup(string AdversaryId, string AdversaryName, IReadOnlyList<int> Levels);

    /// <summary>
    /// Every owned aspect never played, flattened. Counted per variant rather than per spirit —
    /// the same way adversary coverage counts levels, and for the same reason: an aspect is a
    /// different spirit to play. Takes the owned aspects already looked up, so the coverage total
    /// can count the same lists.
    /// </summary>
    public static List<MissingAspect> MissingAspects(
        IReadOnlyList<(Spirit Spirit, IReadOnlyList<Aspect> Aspects)> ownedAspects,
        IEnumerable<SpiritStats> played)
    {
        var playedIds = played
            .SelectMany(s => s.Aspects)
            .Select(a => a.AspectId)
            .OfType<string>()
            .ToHashSet();
        return ownedAspects
            .SelectMany(x => x.Aspects
                .Where(a => !playedIds.Contains(a.Id.Value))
                .Select(a => new MissingAspect(x.Spirit.Id.Value, x.Spirit.Name, a.Name)))
            .ToList();
    }

    /// <summary>The missing aspects grouped under the spirit they vary, spirits in name order.</summary>
    public static List<MissingAspectGroup> GroupAspects(IEnumerable<MissingAspect> missing) =>
        missing
            .GroupBy(m => (m.SpiritId, m.SpiritName))
            .OrderBy(g => g.Key.SpiritName)
            .Select(g => new MissingAspectGroup(g.Key.SpiritId, g.Key.SpiritName, g.Select(m => m.AspectName).ToList()))
            .ToList();

    /// <summary>The missing levels grouped under their foe, foes in name order and levels ascending.</summary>
    public static List<MissingAdversaryGroup> GroupLevels(IEnumerable<LevelCoverage.MissingLevel> missing) =>
        missing
            .GroupBy(m => (m.AdversaryId, m.AdversaryName))
            .OrderBy(g => g.Key.AdversaryName)
            .Select(g => new MissingAdversaryGroup(
                g.Key.AdversaryId, g.Key.AdversaryName, g.Select(m => m.Level).OrderBy(l => l).ToList()))
            .ToList();

    /// <summary>
    /// What was played that the coverage bar above it can't count: content from expansions this
    /// account doesn't own, which happens whenever somebody else brings the box. The record keeps
    /// those games; the collection tally is about the shelf, so it can't. Null when there is
    /// nothing to say.
    /// </summary>
    public static string? OutsideCollectionNote(
        IEnumerable<string> playedIds, IEnumerable<string> ownedIds, string noun)
    {
        var owned = ownedIds.ToHashSet();
        var extra = playedIds.Distinct().Count(id => !owned.Contains(id));
        return extra == 0
            ? null
            : $"Plus {Copy.Count(extra, noun)} played from expansions you don't own.";
    }
}
