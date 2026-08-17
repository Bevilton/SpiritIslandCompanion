namespace Domain.Models.Static;

public class Board
{
    public BoardId Id { get; private init; }
    public string Name { get; private init; }
    public ExpansionId ExpansionId { get; private init; }

    public Board(BoardId id, string name, ExpansionId expansionId)
    {
        Id = id;
        Name = name;
        ExpansionId = expansionId;
    }
}