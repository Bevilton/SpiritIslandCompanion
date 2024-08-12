namespace Domain.Models.Static;

public class Screnario
{
    public ScenarioId Id { get; init; }
    public string Name { get; init; }
    public int Difficulty { get; init; }
    public ExpansionId ExpansionId { get; init; }

    public Screnario(ScenarioId id, string name, int difficulty, ExpansionId expansionId)
    {
        Id = id;
        Name = name;
        Difficulty = difficulty;
        ExpansionId = expansionId;
    }
}