using Domain.Primitives;
using Domain.Services;

namespace Domain.Models.Game;

/// <summary>
/// What a table adds up to. Never supplied by a caller: the only way to get one is
/// <see cref="DifficultyCalculator"/>, which keeps it within <c>[0, MaximumDifficulty]</c>
/// itself — so there is no failure for a difficulty to report, and no public factory to
/// report it through.
/// </summary>
public sealed record Difficulty : ValueObject
{
    public int Value { get; private init; }

    internal Difficulty(int value)
    {
        Value = value;
    }
}
