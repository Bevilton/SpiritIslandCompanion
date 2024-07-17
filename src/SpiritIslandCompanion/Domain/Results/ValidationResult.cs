namespace Domain.Results;

public class ValidationResult : Result, IValidationResult
{
    private ValidationResult(ValidationError[] errors) : base(false, IValidationResult.ValidationError)
    {
        Errors = errors;
    }

    public ValidationError[] Errors { get; }

    public static ValidationResult WithErrors(ValidationError[] errors) => new(errors);
}