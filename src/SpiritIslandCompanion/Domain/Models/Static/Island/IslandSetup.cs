namespace Domain.Models.Static;

public class IslandSetup
{
    public IslandSetupId Id { get; init; }
    public string Name { get; init; }
    public int NumberOfPlayers { get; init; }
    public bool IsThematic { get; init; }

    public IslandSetup(IslandSetupId id, string name, int numberOfPlayers, bool isThematic = false)
    {
        Id = id;
        Name = name;
        NumberOfPlayers = numberOfPlayers;
        IsThematic = isThematic;
    }
}
