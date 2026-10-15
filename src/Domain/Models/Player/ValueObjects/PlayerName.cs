using Domain.Errors;
using Domain.Primitives;
using Domain.Results;

namespace Domain.Models.Player;

public sealed record PlayerName : ValueObject
{
    public const int MaxLength = 100;

    public string Value { get; private init; }

    private PlayerName(string value)
    {
        Value = value;
    }

    public static Result<PlayerName> Create(string? name)
    {
        var trimmed = name?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return Result.Failure<PlayerName>(DomainErrors.Player.NameRequired);
        if (trimmed.Length > MaxLength)
            return Result.Failure<PlayerName>(DomainErrors.Player.NameTooLong);

        return new PlayerName(trimmed);
    }
}
