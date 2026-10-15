using Domain.Errors;
using Domain.Primitives;
using Domain.Results;

namespace Domain.Models.Game;

public sealed record GameNote : ValueObject
{
    public string Value { get; private init; }

    private GameNote(string value)
    {
        Value = value;
    }

    public static Result<GameNote> Create(string note)
    {
        if (note.Length > GameRestrictions.MaximumNoteLength)
            return Result.Failure<GameNote>(DomainErrors.Game.NoteTooLong);

        return new GameNote(note);
    }

    /// <summary>
    /// A note where one was given, and no note where the field was left out — the shape every
    /// command that carries an optional note needs, so the null branch is written once.
    /// </summary>
    public static Result<GameNote?> CreateOptional(string? note)
    {
        if (note is null)
            return Result.Success<GameNote?>(null);

        var result = Create(note);
        return result.IsSuccess
            ? result.Value
            : Result.Failure<GameNote?>(result.Error);
    }
}
