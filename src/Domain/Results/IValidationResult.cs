namespace Domain.Results;

/// <summary>
/// A failed result that carries the field-bound errors behind it, so a form can show each
/// one against its field rather than as one general message.
/// </summary>
public interface IValidationResult
{
    public static readonly Error ValidationFailed = new("ValidationError", "A validation error occurred.");

    ValidationError[] Errors { get; }
}
