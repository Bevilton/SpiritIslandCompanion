namespace Domain.Models.Static.Data;

public static class Boards
{
    public static IReadOnlyList<Board> All { get; } =
    [
        // Base Game
        new(new("board-a"), "Board A", Expansions.BaseGame),
        new(new("board-b"), "Board B", Expansions.BaseGame),
        new(new("board-c"), "Board C", Expansions.BaseGame),
        new(new("board-d"), "Board D", Expansions.BaseGame),

        // Jagged Earth
        new(new("board-e"), "Board E", Expansions.JaggedEarth),
        new(new("board-f"), "Board F", Expansions.JaggedEarth),

        // Horizons of Spirit Island
        new(new("board-g"), "Board G", Expansions.HorizonsOfSpiritIsland),
        new(new("board-h"), "Board H", Expansions.HorizonsOfSpiritIsland),
    ];
}
