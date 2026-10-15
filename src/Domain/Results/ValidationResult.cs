namespace Domain.Results;

public sealed class ValidationResult : Result, IValidationResult
{
    private ValidationResult(ValidationError[] errors) : base(false, IValidationResult.ValidationFailed)
    {
        Errors = errors;
    }

    public ValidationError[] Errors { get; }

    public static ValidationResult WithErrors(ValidationError[] errors) => new(errors);
}
