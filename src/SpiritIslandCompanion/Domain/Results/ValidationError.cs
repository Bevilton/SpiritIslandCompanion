namespace Domain.Results;

public class ValidationError
{
    public string Property { get; }
    public string Code { get; }
    public string Message { get; }

    public ValidationError(string property, string code, string message)
    {
        Property = property;
        Code = code;
        Message = message;
    }
}