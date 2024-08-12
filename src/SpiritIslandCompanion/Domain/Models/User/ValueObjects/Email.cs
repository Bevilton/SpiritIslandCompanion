using Domain.Primitives;

namespace Domain.Models.User;

public record Email : ValueObject
{
    public string Value { get; private init; }

    private Email(string value)
    {
        Value = value;
    }

    public static Email Create(string email)
    {
        return new Email(email);
    }
}