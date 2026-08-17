namespace Domain.Results;

public interface IValidationResult : IResult
{
    public static readonly Error ValidationError = Error.Validation("ValidationError", "A validation error occurred.");

    public ValidationError[] Errors { get; }
}