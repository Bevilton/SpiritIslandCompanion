using Domain.Models.Static;
using Domain.Models.Static.Data;

namespace WebApp.Components.Shared.Games;

/// <summary>
/// A catalogue narrowed to the expansions on the reader's shelf — the one question every browse
/// page, picker and coverage tally had been answering for itself, in eight copies of the same
/// <c>Where(x =&gt; owned is null || owned.Contains(x.ExpansionId))</c>.
/// <para>
/// Null owned expansions means "show everything", not "show nothing": that is what the
/// <c>OwnedExpansions</c> cascade carries for an anonymous visitor and for the moment before it
/// has loaded, and a page that filtered to the empty set there would flash an empty catalogue at
/// a signed-in player on every navigation.
/// </para>
/// </summary>
public static class OwnedCatalogue
{
    /// <summary>Whether an expansion is on the shelf. See the note above on null.</summary>
    public static bool Owns(this IReadOnlyList<ExpansionId>? owned, ExpansionId expansion) =>
        owned is null || owned.Contains(expansion);

    /// <summary>The part of any catalogue that is on the shelf, given how its items name their
    /// expansion. The typed members below cover the five catalogues the app actually browses.</summary>
    private static IReadOnlyList<T> OwnedPart<T>(
        this IReadOnlyList<ExpansionId>? owned,
        IEnumerable<T> catalogue,
        Func<T, ExpansionId> expansion) =>
        catalogue.Where(item => owned.Owns(expansion(item))).ToList();

    public static IReadOnlyList<Spirit> Spirits(IReadOnlyList<ExpansionId>? owned) =>
        owned.OwnedPart(GameData.Spirits, s => s.ExpansionId);

    public static IReadOnlyList<Board> Boards(IReadOnlyList<ExpansionId>? owned) =>
        owned.OwnedPart(GameData.Boards, b => b.ExpansionId);

    public static IReadOnlyList<Adversary> Adversaries(IReadOnlyList<ExpansionId>? owned) =>
        owned.OwnedPart(GameData.Adversaries, a => a.ExpansionId);

    public static IReadOnlyList<Scenario> Scenarios(IReadOnlyList<ExpansionId>? owned) =>
        owned.OwnedPart(GameData.Scenarios, s => s.ExpansionId);

    /// <summary>One spirit's aspects that are on the shelf — an aspect ships in its own box.</summary>
    public static IReadOnlyList<Aspect> AspectsFor(SpiritId spirit, IReadOnlyList<ExpansionId>? owned) =>
        owned.OwnedPart(GameData.GetAspectsForSpirit(spirit), a => a.ExpansionId);
}
