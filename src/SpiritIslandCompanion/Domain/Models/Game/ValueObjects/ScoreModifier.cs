using Domain.Errors;
using Domain.Primitives;
using Domain.Results;

namespace Domain.Models.Game;

public record ScoreModifier : ValueObject
{
    public int Value { get; private init; }

    private ScoreModifier(int value)
    {
        Value = value;
    }

    public static Result<ScoreModifier> Create(int value)
    {
        if (value is < 0 or > GameRestrictions.MaximumScoreModifier)
            return Result.Failure<ScoreModifier>(DomainErrors.Game.InvalidScoreModifier);

        return new ScoreModifier(value);
    }
}