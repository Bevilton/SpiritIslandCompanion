using Domain.Errors;
using Domain.Primitives;
using Domain.Results;

namespace Domain.Models.Game;

public sealed record DifficultyModifier : ValueObject
{
    public int Value { get; private init; }

    private DifficultyModifier(int value)
    {
        Value = value;
    }

    public static Result<DifficultyModifier> Create(int value)
    {
        if (value is < GameRestrictions.MinimumDifficultyModifier or > GameRestrictions.MaximumDifficultyModifier)
            return Result.Failure<DifficultyModifier>(DomainErrors.Game.InvalidDifficultyModifier);

        return new DifficultyModifier(value);
    }
}
