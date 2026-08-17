namespace Domain.Models.Static.Data;

/// <summary>
/// Which board goes where on the thematic island.
/// <para>
/// The thematic map is the real island, so a board isn't a free choice: each one is a
/// specific region and only fits its own place. The lists below are in <em>slot order</em> —
/// the same order the layout's placements appear in
/// <c>wwwroot/js/island-geometry.js</c> — so the board at index <c>i</c> belongs on the
/// island position the playground puts board <c>i</c> at, and the seat at that position
/// plays it.
/// </para>
/// <para>
/// Positions are as the island is <em>drawn</em>, which for thematic layouts is mirrored
/// left-to-right: the boards are played on their reverse side, and turning a piece over
/// reflects it (see <c>state.mirrored</c> in the playground). Slot numbering is unaffected by
/// the reflection, but which side of the screen a slot appears on is — so these pairings only
/// make sense together with the mirrored view.
/// </para>
/// <para>
/// Each region is named in <see cref="BoardDetails"/> (<c>ThematicName</c>), and the six of
/// them tile the island as three rows of two. The check that catches a wrong pairing: a
/// board's coast faces its own compass direction, so every board on the island's east side
/// has its ocean to the east and every one on the west side has it to the west.
/// </para>
/// </summary>
public static class ThematicIslandBoards
{
    private static readonly Dictionary<string, IReadOnlyList<BoardId>> _bySetup = new(StringComparer.Ordinal)
    {
        //                                                position as drawn (mirrored) · coast
        ["thematic-1p"] = [new("board-a")],              // alone            → A North East · east
        ["thematic-2p"] =
        [
            new("board-b"),                             // east             → B East       · east
            new("board-d"),                             // west             → D West       · west
        ],
        // The three-board island is the four-board one without its North West board.
        ["thematic-3p"] =
        [
            new("board-a"),                             // north east       → A North East · east
            new("board-b"),                             // south east       → B East       · east
            new("board-d"),                             // south west       → D West       · west
        ],
        ["thematic-4p"] =
        [
            new("board-a"),                             // north east       → A North East · east
            new("board-c"),                             // north west       → C North West · west
            new("board-b"),                             // south east       → B East       · east
            new("board-d"),                             // south west       → D West       · west
        ],
        // Six boards complete the island: three rows of two, north to south.
        ["thematic-6p"] =
        [
            new("board-a"),                             // north, east      → A North East · east
            new("board-c"),                             // north, west      → C North West · west
            new("board-b"),                             // middle, east     → B East       · east
            new("board-d"),                             // middle, west     → D West       · west
            new("board-e"),                             // south, east      → E South East · east
            new("board-f"),                             // south, west      → F South West · west
        ],
    };

    /// <summary>
    /// The boards for a thematic layout in slot order, or null for any other layout — only
    /// the thematic island fixes which board goes where.
    /// </summary>
    public static IReadOnlyList<BoardId>? For(IslandSetupId? setupId) =>
        setupId is not null ? _bySetup.GetValueOrDefault(setupId.Value) : null;

    /// <summary>
    /// Whether the board is a region of the thematic island at all. The six-board island is
    /// the whole of it, so membership there is membership overall — Horizons' G and H boards
    /// exist only on the standard side and are never on it.
    /// </summary>
    public static bool Includes(BoardId id) => _bySetup["thematic-6p"].Contains(id);
}
