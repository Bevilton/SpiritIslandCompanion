using Domain.Errors;
using Domain.Primitives;
using Domain.Results;

namespace Domain.Models.Game;

public record DifficultyModifier : ValueObject
{
    public int Value { get; private init; }

    private DifficultyModifier(int value)
    {
        Value = value;
    }

    public static Result<DifficultyModifier> Create(int value)
    {
        if (value < GameRestrictions.DifficultyModifierMin || value > GameRestrictions.DifficultyModifierMax)
            return Result.Failure<DifficultyModifier>(DomainErrors.Game.InvalidDifficultyModifier);

        return new DifficultyModifier(value);
    }

    public static DifficultyModifier Zero => new(0);
}
