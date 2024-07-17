namespace Domain.Results;

public sealed class ValidationResult<TValue> : Result<TValue>, IValidationResult
{
    private ValidationResult(ValidationError[] errors) : base(default!, false, IValidationResult.ValidationError)
    {
        Errors = errors;
    }

    public ValidationError[] Errors { get; }

    public static ValidationResult<TValue> WithErrors(ValidationError[] errors) => new(errors);
}