namespace Domain.Results;

/// <summary>
/// What went wrong: a code the app can match on, and the message it shows for it. The kind of
/// failure — not found, refused, invalid — is readable from the code and the message, and
/// nothing reacts to it separately, so it is not carried as a field. Two errors are the same
/// error when they say the same thing under the same code, which is what a record's equality
/// already means.
/// </summary>
public sealed record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);
}
