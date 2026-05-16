using System.Net.Mail;
using Domain.Errors;
using Domain.Primitives;
using Domain.Results;

namespace Domain.Models.User;

public record Email : ValueObject
{
    public string Value { get; private init; }

    private Email(string value)
    {
        Value = value;
    }

    public static Result<Email> Create(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Result.Failure<Email>(DomainErrors.User.EmailRequired);
        if (!IsWellFormed(email))
            return Result.Failure<Email>(DomainErrors.User.EmailInvalid);

        return new Email(email);
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
