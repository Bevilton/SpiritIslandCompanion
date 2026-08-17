using Domain.Errors;
using Domain.Primitives;
using Domain.Results;

namespace Domain.Models.Game;

public record AdversaryLevel : ValueObject
{
    public int Value { get; private init; }

    private AdversaryLevel(int value)
    {
        Value = value;
    }

    public static Result<AdversaryLevel> Create(int value)
    {
        if (value is < 0 or > GameRestrictions.MaximumAdversaryLevel)
            return Result.Failure<AdversaryLevel>(DomainErrors.Game.InvalidAdversaryLevel);

        return new AdversaryLevel(value);
    }
}