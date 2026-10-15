using System.Net.Mail;
using Domain.Errors;
using Domain.Primitives;
using Domain.Results;

namespace Domain.Models.User;

/// <summary>
/// An email address in its canonical form: trimmed and lower-cased. Addresses are matched
/// case-insensitively everywhere in practice, and storing one form is what lets two databases
/// with different collations agree on whether "Anna@…" is anna — the SQL Server that holds
/// the accounts would say yes on its own, the SQLite behind the demo would not.
/// </summary>
public sealed record Email : ValueObject
{
    /// <summary>
    /// Just over the 254 characters an address can be by RFC 5321, so nothing well-formed is
    /// turned away; the point is that the column has a width and the domain says so first.
    /// </summary>
    public const int MaxLength = 256;

    public string Value { get; private init; }

    private Email(string value)
    {
        Value = value;
    }

    public static Result<Email> Create(string? email)
    {
        var canonical = email?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(canonical))
            return Result.Failure<Email>(DomainErrors.User.EmailRequired);
        if (canonical.Length > MaxLength)
            return Result.Failure<Email>(DomainErrors.User.EmailTooLong);
        if (!IsWellFormed(canonical))
            return Result.Failure<Email>(DomainErrors.User.EmailInvalid);

        return new Email(canonical);
    }

    private static bool IsWellFormed(string email)
    {
        try
        {
            var address = new MailAddress(email);
            return address.Address == email;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
