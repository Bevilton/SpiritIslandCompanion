using Domain.Errors;
using Domain.Primitives;
using Domain.Results;

namespace Domain.Models.Game;

public record DahanCount : ValueObject
{
    public int Value { get; private init; }

    private DahanCount(int value)
    {
        Value = value;
    }

    public static Result<DahanCount> Create(int value)
    {
        if (value is < 0 or > GameRestrictions.MaximumDahanCount)
            return Result.Failure<DahanCount>(DomainErrors.Game.InvalidDahanCount);

        return new DahanCount(value);
    }
}