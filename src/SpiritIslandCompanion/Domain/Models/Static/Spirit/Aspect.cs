namespace Domain.Models.Static;

public class Aspect
{
    public AspectId Id { get; init; }
    public string Name { get; init; }
    public ExpansionId ExpansionId { get; init; }

    public Aspect(AspectId id, string name, ExpansionId expansionId)
    {
        Id = id;
        Name = name;
        ExpansionId = expansionId;
    }
}