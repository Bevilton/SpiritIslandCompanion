using Domain.Errors;
using Domain.Primitives;
using Domain.Results;

namespace Domain.Models.Player;

public record PlayerName : ValueObject
{
    public const int MaxLength = 100;

    public string Value { get; private init; }

    private PlayerName(string value)
    {
        Value = value;
    }

    public static Result<PlayerName> Create(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<PlayerName>(DomainErrors.Player.NameRequired);
        if (name.Length > MaxLength)
            return Result.Failure<PlayerName>(DomainErrors.Player.NameTooLong);

        return new PlayerName(name);
    }
}
