namespace Domain.Models.Static.Data;

public sealed record BoardDetail(
    string Letter,
    string ColorHex,
    string ThematicName);

/// <summary>
/// Per-board enrichment data: letter, signature color, and the geographic position
/// the board represents in the thematic island layout.
/// </summary>
public static class BoardDetails
{
    public static IReadOnlyDictionary<BoardId, BoardDetail> All { get; } = new Dictionary<BoardId, BoardDetail>
    {
        // Base game — the four quadrants of the thematic island
        [new("board-a")] = new("A", "#4FB6D9", "Northeast"),
        [new("board-b")] = new("B", "#8B6F47", "Northwest"),
        [new("board-c")] = new("C", "#5BAE5B", "East"),
        [new("board-d")] = new("D", "#C77E47", "West"),

        // Jagged Earth — extend the thematic island for 6-player setups
        [new("board-e")] = new("E", "#6B4F9B", "South"),
        [new("board-f")] = new("F", "#2D8276", "Center"),

        // Horizons — alternate beginner-friendly thematic positions
        [new("board-g")] = new("G", "#E0A04E", "Northeast (alt.)"),
        [new("board-h")] = new("H", "#4A7DA0", "Northwest (alt.)"),
    };

    public static BoardDetail? For(BoardId id) => All.GetValueOrDefault(id);
}
