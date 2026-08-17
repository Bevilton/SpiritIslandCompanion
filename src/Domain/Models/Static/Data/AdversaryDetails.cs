namespace Domain.Models.Static.Data;

public sealed record AdversaryDetail(
    string Theme,
    string Summary);

/// <summary>
/// High-level theme and flavour summaries for adversaries. Conservative
/// descriptions consistent with the Spirit Island wiki and rulebooks —
/// for level-by-level escalation rules, link out to the wiki.
/// </summary>
public static class AdversaryDetails
{
    public static IReadOnlyDictionary<AdversaryId, AdversaryDetail> All { get; } =
        new Dictionary<AdversaryId, AdversaryDetail>
        {
            [Adversaries.BrandenburgPrussia] = new(
                "Militaristic expansion",
                "A militaristic European power that floods the island with Towns and Cities. The classic introductory adversary — straightforward Build pressure that rewards destruction over displacement."),

            [Adversaries.England] = new(
                "Coastal colonization",
                "The archetypal colonial settler. Heavy presence on coastal lands and relentless Explore/Build, with a brutal late game thanks to extra Stage III pressure. A strong test for Spirits used to inland defense."),

            [Adversaries.Sweden] = new(
                "Mining & blighted highlands",
                "Sweden digs into the interior with industrial mining. Mountains and the land itself bear the scars; less concerned with spreading wide than with inflicting deep, lasting damage."),

            [Adversaries.France] = new(
                "Plantation colony",
                "A grim plantation economy reshaping the island around forced labour. Wetlands transform, the Dahan come under direct threat, and standard push-defenses become much harder. Themed for Branch & Claw."),

            [Adversaries.HabsburgMonarchy] = new(
                "Livestock colony",
                "Settlers driving livestock across the wilds, displacing Beasts and reshaping Jungles. Pastoral colonization that grinds slowly but persistently — built for the Jagged Earth box."),

            [Adversaries.Russia] = new(
                "Imperial expansion",
                "Russia pushes hard into the interior with constant serf-driven Town production. Sheer building volume tests Spirits without strong removal."),

            [Adversaries.Scotland] = new(
                "Highlanders",
                "Settlers anchored in Mountains and uplands. The terrain bias gives Scotland a distinctive feel — strong against Spirits who depend on coastal or wetland play."),

            [Adversaries.HabsburgMining] = new(
                "Mining expedition",
                "A focused expedition extracting precious metals from specific lands. Narrower in scope than a full colonization, but devastating in the regions it targets. Themed for Nature Incarnate."),
        };

    public static AdversaryDetail? For(AdversaryId id) => All.GetValueOrDefault(id);
}
