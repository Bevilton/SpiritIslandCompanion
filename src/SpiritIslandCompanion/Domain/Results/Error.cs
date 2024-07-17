namespace Domain.Results;

public class Error : IEquatable<Error>
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);
    public string Code { get; }
    public string Message { get; }
    public ErrorType Type { get; }

    private Error(string code, string message, ErrorType type)
    {
        Code = code;
        Message = message;
        Type = type;
    }

    public bool Equals(Error? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Code == other.Code && Message == other.Message;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((Error)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Code, Message);
    }

    public static Error NotFound(string code, string message)
    {
        return new Error(code, message, ErrorType.NotFound);
    }

    public static Error Conflict(string code, string message)
    {
        return new Error(code, message, ErrorType.Conflict);
    }

    public static Error Validation(string code, string message)
    {
        return new Error(code, message, ErrorType.Validation);
    }

    public static Error Failure(string code, string message)
    {
        return new Error(code, message, ErrorType.Failure);
    }

    public static bool operator ==(Error? left, Error? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Error? left, Error? right)
    {
        return !Equals(left, right);
    }
}