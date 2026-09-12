using System.Collections.Frozen;
using Application.Features.Games.Dtos;
using Domain.Models.Static;
using Domain.Models.Static.Data;

namespace WebApp.Components.Shared.Games;

/// <summary>
/// Lookups from the static <see cref="GameData"/> tables by their string id.
/// Shared by every page/component that renders persisted game data — they all
/// store the entity id and need the name, color, image, etc. for display.
/// </summary>
public static class GameLookups
{
    /// <summary>Fallback swatch for an entity without a catalogue colour of its own.
    /// Kept in step with ink-500 in Styles/site.css, which is the warm parchment neutral.</summary>
    public const string NeutralColor = "#7a6c4e";

    /// <summary>
    /// The swatch a spirit is drawn in wherever it shrinks to a dot, a bar or a chip: its
    /// catalogue colour, or <see cref="NeutralColor"/> for one without a detail entry — or no
    /// spirit at all. The null-tolerant fallback was the same two-step dance at a dozen call
    /// sites; callers that already hold the detail for its other fields keep reading it directly.
    /// </summary>
    public static string SpiritColor(Spirit? spirit) => SpiritColor(DetailFor(spirit));

    /// <summary>
    /// The same colour, left null where the spirit has none — for the callers where "no colour"
    /// means no mark at all rather than a neutral one, as on the statistics highlight cards,
    /// whose accent bar is simply left off. Three of them spelled the two-step out.
    /// </summary>
    public static string? SpiritAccent(Spirit? spirit) => DetailFor(spirit)?.ColorHex;

    /// <summary>A board's swatch, by the same rule as <see cref="SpiritColor(Spirit?)"/>.</summary>
    public static string BoardColor(Board? board) => BoardColor(DetailFor(board));

    /// <summary>
    /// The same swatch for a caller already holding the catalogue entry for its other fields —
    /// the play style, the elements. Seventeen call sites had written the fallback out as
    /// <c>detail?.ColorHex ?? GameLookups.NeutralColor</c>, which is the rule the two lookups
    /// above exist to keep in one place.
    /// </summary>
    public static string SpiritColor(SpiritDetail? detail) => detail?.ColorHex ?? NeutralColor;

    /// <inheritdoc cref="SpiritColor(SpiritDetail?)"/>
    public static string BoardColor(BoardDetail? detail) => detail?.ColorHex ?? NeutralColor;

    /// <summary>
    /// The style a badge tinted by its subject wears — the subject's colour over a thin wash of
    /// itself. The suffix is alpha: 12%, enough to read as a tint rather than a fill. Three
    /// badges had spelled it out and drifted a step apart on the wash.
    /// </summary>
    public static string TintBadgeStyle(string hexColor) =>
        $"background-color: {hexColor}20; color: {hexColor};";

    /// <summary>
    /// The band a subject with a colour of its own heads its record sheet with: a wash of that
    /// colour fading across, with the border a stronger tint of the same. The hex suffixes are
    /// alpha — barely there at the top-left, gone by the right, and firm enough on the border to
    /// read as an edge. Here beside <see cref="TintBadgeStyle"/> rather than on the header
    /// component that wears it: both are one subject's colour turned into a style, and the two
    /// sheets that need it reached through the component to get it.
    /// </summary>
    public static string TintBandStyle(string hexColor) =>
        $"background: linear-gradient(135deg, {hexColor}18 0%, {hexColor}08 100%); " +
        $"border-bottom-color: {hexColor}33;";

    /// <summary>
    /// Every lookup below answers the same question — "which entry carries this string id?" —
    /// and each was answering it by scanning the whole catalogue. That is nothing once and a
    /// great deal in the places these are actually called: a statistics table resolves an entry,
    /// its detail and its colour per row on every render, and the games list resolves a foe per
    /// foe per game. Home.razor had already noticed and grown three private dictionaries of its
    /// own, which is the shape a missing index takes when nobody builds it.
    /// <para>
    /// One index per catalogue, over static readonly data, so it is built once for the process.
    /// Keyed on the raw string every caller already holds rather than on the id wrapper they
    /// would otherwise have to construct.
    /// </para>
    /// <para>
    /// A duplicate id throws here rather than being quietly resolved to whichever entry came
    /// first, which is what the scan used to do. That is deliberate: two catalogue entries
    /// answering to one id is a data bug that would otherwise surface as a spirit occasionally
    /// rendering as another one, and it fails on the first lookup rather than in a screenshot.
    /// </para>
    /// </summary>
    private static FrozenDictionary<string, T> Index<T>(IEnumerable<T> catalogue, Func<T, string> id) =>
        catalogue.ToFrozenDictionary(id, StringComparer.Ordinal);

    private static readonly FrozenDictionary<string, Spirit> SpiritsById =
        Index(GameData.Spirits, s => s.Id.Value);
    private static readonly FrozenDictionary<string, Board> BoardsById =
        Index(GameData.Boards, b => b.Id.Value);
    private static readonly FrozenDictionary<string, Adversary> AdversariesById =
        Index(GameData.Adversaries, a => a.Id.Value);
    private static readonly FrozenDictionary<string, Scenario> ScenariosById =
        Index(GameData.Scenarios, s => s.Id.Value);
    private static readonly FrozenDictionary<string, IslandSetup> IslandSetupsById =
        Index(GameData.IslandSetups, s => s.Id.Value);
    private static readonly FrozenDictionary<string, Aspect> AspectsById =
        Index(GameData.Aspects, a => a.Id.Value);
    private static readonly FrozenDictionary<string, Expansion> ExpansionsById =
        Index(GameData.Expansions, e => e.Id.Value);

    /// <summary>
    /// The catalogue entry behind a recorded id, or null where there is none — a field not filled
    /// in yet, or something played out of a box the catalogue has since dropped. Null-tolerant on
    /// purpose: every caller holds an id that may legitimately be empty, and the alternative is
    /// the same guard written at each of them.
    /// </summary>
    public static Spirit? SpiritFor(string? id) => Find(SpiritsById, id);

    /// <inheritdoc cref="SpiritFor"/>
    public static Board? BoardFor(string? id) => Find(BoardsById, id);

    /// <inheritdoc cref="SpiritFor"/>
    public static Adversary? AdversaryFor(string? id) => Find(AdversariesById, id);

    /// <inheritdoc cref="SpiritFor"/>
    public static Scenario? ScenarioFor(string? id) => Find(ScenariosById, id);

    /// <inheritdoc cref="SpiritFor"/>
    public static IslandSetup? IslandSetupFor(string? id) => Find(IslandSetupsById, id);

    /// <inheritdoc cref="SpiritFor"/>
    public static Aspect? AspectFor(string? id) => Find(AspectsById, id);

    private static T? Find<T>(FrozenDictionary<string, T> index, string? id) where T : class =>
        string.IsNullOrEmpty(id) ? null : index.GetValueOrDefault(id);

    /// <summary>
    /// The catalogue's own entry for a subject the catalogue may not list — its colour, its play
    /// style, its thematic name, the rest of what a card and a sheet are built from.
    /// <para>
    /// <c>SpiritDetails.For</c> and <c>BoardDetails.For</c> take an id, and every caller here is
    /// one step further back: they hold the <see cref="Spirit"/> or <see cref="Board"/> that
    /// <see cref="SpiritFor"/> just resolved, and it is legitimately null — a seat not filled in
    /// yet, or something played out of a box the catalogue has since dropped. So ten call sites
    /// across nine files wrote the same <c>x is null ? null : XDetails.For(x.Id)</c>, which is the
    /// guard the lookups above exist to stop being written per caller. Same rule, one step down.
    /// </para>
    /// </summary>
    public static SpiritDetail? DetailFor(Spirit? spirit) =>
        spirit is null ? null : SpiritDetails.For(spirit.Id);

    /// <inheritdoc cref="DetailFor(Spirit?)"/>
    public static BoardDetail? DetailFor(Board? board) =>
        board is null ? null : BoardDetails.For(board.Id);

    /// <summary>What an expansion is called — its own id where the catalogue has no entry.</summary>
    public static string ExpansionName(ExpansionId id) =>
        ExpansionsById.GetValueOrDefault(id.Value)?.Name ?? id.Value;

    /// <summary>A board's letter — from the catalogue detail, or the name's last character
    /// for a board that has no detail entry.</summary>
    public static string BoardLetter(Board board) =>
        BoardDetails.For(board.Id)?.Letter ?? board.Name[^1..];

    /// <summary>
    /// The letter of a board held only by its recorded id — the extra board on a setup, a board
    /// facet on the games list. Null where the id names nothing, which is the caller's cue to say
    /// the board was not recorded rather than to print a letter it cannot know.
    /// </summary>
    public static string? BoardLetterFor(string? boardId) =>
        BoardFor(boardId) is { } board ? BoardLetter(board) : null;

    /// <summary>
    /// The two letters a spirit wears wherever it shrinks to a badge — the catalogue entry's
    /// hand-picked code, unique across all spirits. Only a spirit without a detail entry (played
    /// from outside the catalogue) falls back to deriving one from its name: first letters of
    /// the first two significant words, or the first two letters of a one-word name. No spirit
    /// at all — an id the catalogue no longer lists — is a question mark, which both callers that
    /// draw one had written out for themselves.
    /// </summary>
    public static string SpiritMonogram(Spirit? spirit) =>
        spirit is null ? "?" : SpiritDetails.For(spirit.Id)?.Monogram ?? DeriveMonogram(spirit.Name);

    private static string DeriveMonogram(string name)
    {
        var words = name.Split([' ', '-'], StringSplitOptions.RemoveEmptyEntries)
            .Where(w => !MonogramSkipWords.Contains(w))
            .ToList();
        return words.Count switch
        {
            0 => "?",
            1 => words[0][..Math.Min(2, words[0].Length)],
            _ => $"{char.ToUpperInvariant(words[0][0])}{char.ToUpperInvariant(words[1][0])}",
        };
    }

    private static readonly HashSet<string> MonogramSkipWords =
        new(StringComparer.OrdinalIgnoreCase) { "a", "an", "as", "and", "in", "its", "of", "the", "up", "your" };

    /// <summary>What a match with no name of its own is called — also the word the setup card's
    /// difficulty breakdown uses for a game nothing added to. Ask <see cref="IsStandardGame"/>
    /// rather than comparing against this.</summary>
    public const string StandardGameTitle = "Standard game";

    /// <summary>
    /// Whether the title <see cref="MatchTitle"/> returned is the stand-in rather than a name the
    /// match actually has.
    /// <para>
    /// Asked of the title rather than of the game, which is the whole point: four call sites asked
    /// it three different ways — two of them <c>Adversaries.Count == 0 &amp;&amp; ScenarioId is
    /// null</c>, one the same over a differently-named list, and one a comparison against the
    /// stand-in. Those are not the same question. A game whose foe was played from a box the
    /// catalogue has since dropped has adversaries and still has no name, because
    /// <see cref="MatchTitle"/> can't resolve one — so three of the four set an unnamed match in
    /// bold ink while the fourth set it muted and italic. <see cref="MatchTitle"/> is what decides
    /// whether a match is named, so it is what this reads.
    /// </para>
    /// </summary>
    public static bool IsStandardGame(string title) => title == StandardGameTitle;

    /// <summary>
    /// How a match's title is set wherever one is listed. A game with no name of its own is set
    /// muted and italic, to say that <see cref="StandardGameTitle"/> is a description rather than
    /// a name.
    /// <para>
    /// Three lists draw that distinction and each spelled the pair of tones out; the game page
    /// makes the same point with <c>PageHeader.TitleClass="italic"</c>, where the heading already
    /// carries its own size and weight.
    /// </para>
    /// </summary>
    public static string MatchTitleTone(string title) =>
        IsStandardGame(title) ? "font-normal italic text-ink-500" : "font-semibold text-ink-900";

    /// <summary>
    /// What a recorded game is called wherever one is listed: the adversaries fought, or the
    /// scenario when there were none, or "Standard game" when there was neither. One
    /// helper so the games list, the game detail and anything else that names a match agree.
    /// </summary>
    public static string MatchTitle(IReadOnlyList<string> adversaryIds, string? scenarioId)
    {
        var adversaries = adversaryIds
            .Select(id => AdversaryFor(id)?.Name)
            .Where(n => n is not null)
            .ToList();
        if (adversaries.Count > 0) return string.Join(" + ", adversaries);
        return ScenarioFor(scenarioId)?.Name ?? StandardGameTitle;
    }

    /// <summary>
    /// <see cref="MatchTitle(IReadOnlyList{string}, string?)"/> with the levels fought, for
    /// callers whose data carries them: "England L3 + Russia L2".
    /// </summary>
    public static string MatchTitle(IReadOnlyList<GameAdversaryResponse> adversaries, string? scenarioId) =>
        AdversaryLabel(adversaries) ?? ScenarioFor(scenarioId)?.Name ?? StandardGameTitle;

    /// <summary>The adversaries with their levels, "England L3 + Russia L2" — null when none.</summary>
    private static string? AdversaryLabel(IReadOnlyList<GameAdversaryResponse> adversaries)
    {
        var named = adversaries
            .Select(a => AdversaryFor(a.AdversaryId) is { } adv ? $"{adv.Name} L{a.Level}" : null)
            .Where(n => n is not null)
            .ToList();
        return named.Count > 0 ? string.Join(" + ", named) : null;
    }

    /// <summary>
    /// The badge a scenario's added difficulty wears: neutral for one that changes the rules
    /// without making them harder, info up to +2, warn from +3. Written out as the same nested
    /// ternary on the reference table, the picker and the record sheet, so the three could have
    /// drifted apart on where "hard" starts.
    /// </summary>
    public static string DifficultyBadge(int difficulty) =>
        difficulty >= 3 ? "badge-warn"
        : difficulty >= 1 ? "badge-info"
        : "badge-neutral";

    /// <summary>The Spirit Island wiki page for a catalogue entity, by its display name.</summary>
    public static string WikiLink(string name)
    {
        var slug = name.Replace("'", "%27").Replace(" ", "_");
        return $"https://spiritislandwiki.com/index.php?title={slug}";
    }
}
