namespace Domain.Results;

public class Result<TValue> : Result
{
    private readonly TValue? _value;

    protected internal Result(TValue? value, bool isSuccess, Error error) : base(isSuccess, error)
    {
        _value = value;
    }

    public TValue Value => IsSuccess ? _value! : throw new InvalidOperationException("Cannot access value of a failure result.");

    public static implicit operator Result<TValue>(TValue t) => new(t, true, Error.None);
}