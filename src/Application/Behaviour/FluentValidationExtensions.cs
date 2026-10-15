using Domain.Results;
using FluentValidation;

namespace Application.Behaviour;

public static class FluentValidationExtensions
{
    /// <summary>
    /// Bridges a FluentValidation rule to an <see cref="Error"/> defined in the domain, so the
    /// user-facing copy and the error code live in one place instead of on each rule.
    /// </summary>
    public static IRuleBuilderOptions<T, TProperty> WithDomainError<T, TProperty>(
        this IRuleBuilderOptions<T, TProperty> rule, Error error)
        => rule.WithErrorCode(error.Code).WithMessage(error.Message);

    /// <summary>
    /// A length limit measured the way the domain's name value objects measure — after trimming —
    /// so a validator can never refuse a name the domain would accept, or the other way round.
    /// </summary>
    public static IRuleBuilderOptions<T, string> MaximumTrimmedLength<T>(
        this IRuleBuilder<T, string> rule, int maxLength)
        => rule.Must(value => value is null || value.Trim().Length <= maxLength);
}
