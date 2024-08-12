namespace Domain.Models.Static;

public class Expansion
{
    public ExpansionId Id { get; private init; }
    public string Name { get; private init; }

    public Expansion(ExpansionId id, string name)
    {
        Id = id;
        Name = name;
    }
}