namespace Domain.Models.Static.Data;

public static class IslandSetups
{
    public static IReadOnlyList<IslandSetup> All { get; } =
    [
        // Base Game layouts
        new(new("standard-1p"), "Standard", 1),
        new(new("standard-2p"), "Standard", 2),
        new(new("coastline-2p"), "Coastline", 2),
        new(new("standard-3p"), "Standard", 3),
        new(new("standard-4p"), "Standard", 4),

        // Jagged Earth layouts
        new(new("fragment-2p"), "Fragment", 2),
        new(new("opposite-shores-2p"), "Opposite Shores", 2),
        new(new("coastline-3p"), "Coastline", 3),
        new(new("sunrise-3p"), "Sunrise", 3),
        new(new("leaf-4p"), "Leaf", 4),
        new(new("snake-4p"), "Snake", 4),
        new(new("crab-5p"), "Crab", 5),
        new(new("claw-5p"), "Claw", 5),
        new(new("peninsula-5p"), "Peninsula", 5),
        new(new("snail-5p"), "Snail", 5),
        new(new("v-5p"), "V", 5),
        new(new("two-centers-6p"), "Two Centers", 6),
        new(new("caldera-6p"), "Caldera", 6),
        new(new("flower-6p"), "Flower", 6),
        new(new("star-6p"), "Star", 6),
    ];
}
