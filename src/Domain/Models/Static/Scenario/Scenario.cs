namespace Domain.Models.Static;

public class Scenario
{
    public ScenarioId Id { get; init; }
    public string Name { get; init; }
    public int Difficulty { get; init; }
    public ExpansionId ExpansionId { get; init; }

    public Scenario(ScenarioId id, string name, int difficulty, ExpansionId expansionId)
    {
        Id = id;
        Name = name;
        Difficulty = difficulty;
        ExpansionId = expansionId;
    }
}
