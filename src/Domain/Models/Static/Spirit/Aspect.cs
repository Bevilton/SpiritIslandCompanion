namespace Domain.Models.Static;

public class Aspect
{
    public AspectId Id { get; init; }
    public string Name { get; init; }
    public SpiritId SpiritId { get; init; }
    public ExpansionId ExpansionId { get; init; }

    public Aspect(AspectId id, string name, SpiritId spiritId, ExpansionId expansionId)
    {
        Id = id;
        Name = name;
        SpiritId = spiritId;
        ExpansionId = expansionId;
    }
}
