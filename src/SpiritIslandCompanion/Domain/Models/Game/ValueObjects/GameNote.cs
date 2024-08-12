using Domain.Errors;
using Domain.Primitives;
using Domain.Results;

namespace Domain.Models.Game;

public record GameNote : ValueObject
{
    public string Value { get; }

    private GameNote(string value)
    {
        Value = value;
    }

    public static Result<GameNote> Create(string note)
    {
        if (note.Length > GameRestrictions.NoteLength)
            return Result.Failure<GameNote>(DomainErrors.Game.NoteTooLong);

        return new GameNote(note);
    }
}