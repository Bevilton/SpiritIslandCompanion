namespace Domain.Models.Static;

public sealed record Scenario(ScenarioId Id, string Name, int Difficulty, ExpansionId ExpansionId);
