using Domain.Primitives;

namespace Domain.Models.Player;

public record PlayerName : ValueObject
{
    public string Value { get; private init; }

    private PlayerName(string value)
    {
        Value = value;
    }

    public static PlayerName Create(string name) => new(name);
}