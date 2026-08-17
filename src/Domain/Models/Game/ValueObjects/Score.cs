using Domain.Errors;
using Domain.Primitives;
using Domain.Results;

namespace Domain.Models.Game;

public record Score : ValueObject
{
    public int Value { get; private init; }

    private Score(int value)
    {
        Value = value;
    }

    public static Result<Score> Create(int value)
    {
        if (value is < 0 or > GameRestrictions.MaximumScore)
            return Result.Failure<Score>(DomainErrors.Game.InvalidScore);

        return new Score(value);
    }
}