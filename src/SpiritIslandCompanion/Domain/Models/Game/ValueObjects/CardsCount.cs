using Domain.Errors;
using Domain.Primitives;
using Domain.Results;

namespace Domain.Models.Game;

public record CardsCount : ValueObject
{
    public int Value { get; private init; }

    private CardsCount(int value)
    {
        Value = value;
    }

    public static Result<CardsCount> Create(int value)
    {
        if (value is < 0 or > GameRestrictions.MaximumCardsCount)
            return Result.Failure<CardsCount>(DomainErrors.Game.InvalidCardCount);

        return new CardsCount(value);
    }
}