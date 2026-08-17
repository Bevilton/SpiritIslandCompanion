namespace Domain.Models.Static;

public class Spirit
{
    public SpiritId Id { get; private init; }
    public string Name { get; private init; }
    public ExpansionId ExpansionId { get; private init; }

    public Spirit(SpiritId id, string name, ExpansionId expansionId)
    {
        Id = id;
        Name = name;
        ExpansionId = expansionId;
    }
}