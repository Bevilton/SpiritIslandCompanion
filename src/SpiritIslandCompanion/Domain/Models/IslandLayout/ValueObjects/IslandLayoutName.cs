using Domain.Errors;
using Domain.Primitives;
using Domain.Results;

namespace Domain.Models.IslandLayout;

/// <summary>
/// What the player calls a shape they built and want to use again — "Sunrise Bay",
/// "the long one". Short on purpose: it has to fit on a layout tile in the picker.
/// </summary>
public record IslandLayoutName : ValueObject
{
    public const int MaxLength = 40;

    public string Value { get; private init; }

    private IslandLayoutName(string value)
    {
        Value = value;
    }

    public static Result<IslandLayoutName> Create(string? name)
    {
        var trimmed = name?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return Result.Failure<IslandLayoutName>(DomainErrors.IslandLayout.NameRequired);
        if (trimmed.Length > MaxLength)
            return Result.Failure<IslandLayoutName>(DomainErrors.IslandLayout.NameTooLong);

        return new IslandLayoutName(trimmed);
    }
}
