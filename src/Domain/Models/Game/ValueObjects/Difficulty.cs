using Domain.Errors;
using Domain.Primitives;
using Domain.Results;

namespace Domain.Models.Game;

public record Difficulty : ValueObject
{
    public int Value { get; private init; }

    private Difficulty(int value)
    {
        Value = value;
    }

    public static Result<Difficulty> Create(int value)
    {
        if (value is < 0 or > GameRestrictions.MaximumDifficulty)
            return Result.Failure<Difficulty>(DomainErrors.Game.InvalidDifficulty);

        return new Difficulty(value);
    }
}