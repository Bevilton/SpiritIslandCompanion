namespace Domain.Models.Static;

public class Adversary
{
    public AdversaryId Id { get; private init; }
    public string Name { get; private init; }
    public ExpansionId ExpansionId { get; private init; }
    public IEnumerable<Mode> Modes { get; private init; }

    public Adversary(AdversaryId id, string name, ExpansionId expansionId, IEnumerable<Mode> modes)
    {
        Id = id;
        Name = name;
        ExpansionId = expansionId;
        Modes = modes;
    }
}