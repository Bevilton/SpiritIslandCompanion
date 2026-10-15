using Domain.Primitives;
using Domain.Services;

namespace Domain.Models.Game;

/// <summary>
/// What a finished game scored. Never supplied by a caller: the only way to get one is
/// <see cref="ScoreCalculator"/>, which keeps it within <c>[0, MaximumScore]</c> itself —
/// so there is no failure for a score to report, and no public factory to report it through.
/// </summary>
public sealed record Score : ValueObject
{
    public int Value { get; private init; }

    internal Score(int value)
    {
        Value = value;
    }
}
