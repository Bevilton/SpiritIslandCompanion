namespace Domain.Models.Static.Data;

public sealed record BoardDetail(
    string Letter,
    string ColorHex,
    string ThematicName);

/// <summary>
/// Per-board enrichment data: letter, signature color, and the region of the thematic
/// island the board is.
/// <para>
/// The regions are those of the board's <em>thematic side</em> — the reverse of the side the
/// standard layouts are played on. Together they tile the island as three rows of two:
/// North West / North East, West / East, South West / South East.
/// </para>
/// </summary>
public static class BoardDetails
{
    public static IReadOnlyDictionary<BoardId, BoardDetail> All { get; } = new Dictionary<BoardId, BoardDetail>
    {
        // Base game — the northern and middle rows of the thematic island
        [new("board-a")] = new("A", "#4FB6D9", "North East"),
        [new("board-b")] = new("B", "#8B6F47", "East"),
        [new("board-c")] = new("C", "#5BAE5B", "North West"),
        [new("board-d")] = new("D", "#C77E47", "West"),

        // Jagged Earth — the southern row, which completes the island for 6 boards
        [new("board-e")] = new("E", "#6B4F9B", "South East"),
        [new("board-f")] = new("F", "#2D8276", "South West"),

        // Horizons — alternate beginner-friendly boards, not part of the thematic island
        [new("board-g")] = new("G", "#E0A04E", "North East (alt.)"),
        [new("board-h")] = new("H", "#4A7DA0", "North West (alt.)"),
    };

    public static BoardDetail? For(BoardId id) => All.GetValueOrDefault(id);
}
