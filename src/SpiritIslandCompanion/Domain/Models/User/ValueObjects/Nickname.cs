using Domain.Errors;
using Domain.Primitives;
using Domain.Results;

namespace Domain.Models.User;

public record Nickname : ValueObject
{
    public const int MaxLength = 50;

    public string Value { get; private init; }

    private Nickname(string value)
    {
        Value = value;
    }

    public static Result<Nickname> Create(string? nickname)
    {
        if (string.IsNullOrWhiteSpace(nickname))
            return Result.Failure<Nickname>(DomainErrors.User.NicknameRequired);
        if (nickname.Length > MaxLength)
            return Result.Failure<Nickname>(DomainErrors.User.NicknameTooLong);

        return new Nickname(nickname);
    }
}
