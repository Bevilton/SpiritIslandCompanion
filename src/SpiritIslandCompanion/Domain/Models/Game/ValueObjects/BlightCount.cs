using Domain.Errors;
using Domain.Primitives;
using Domain.Results;

namespace Domain.Models.Game;

public record BlightCount : ValueObject
{
    public int Value { get; private init; }

    private BlightCount(int value)
    {
        Value = value;
    }

    public static Result<BlightCount> Create(int value)
    {
        if (value is < 0 or > GameRestrictions.MaximumBlightCount)
            Result.Failure<BlightCount>(DomainErrors.Game.InvalidBlightCount);

        return new BlightCount(value);
    }
}