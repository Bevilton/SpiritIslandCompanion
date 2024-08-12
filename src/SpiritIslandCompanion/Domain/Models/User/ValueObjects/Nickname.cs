using Domain.Primitives;

namespace Domain.Models.User;

public record Nickname : ValueObject
{
    public string Value { get; private init; }

    private Nickname(string value)
    {
        Value = value;
    }

    public static Nickname Create(string nickname)
    {
        return new Nickname(nickname);
    }
}