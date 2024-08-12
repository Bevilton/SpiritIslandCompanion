namespace Domain.Models.Static;

public class IslandSetup
{
    public IslandSetupId Id { get; init; }
    public string Name { get; init; }
    public int NumberOfPlayers { get; init; }

    public IslandSetup(IslandSetupId id, string name, int numberOfPlayers)
    {
        Id = id;
        Name = name;
        NumberOfPlayers = numberOfPlayers;
    }
}