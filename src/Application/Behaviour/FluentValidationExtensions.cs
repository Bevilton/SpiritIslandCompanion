using Domain.Results;
using FluentValidation;

namespace Application.Behaviour;

/// <summary>
/// Bridges a FluentValidation rule to a <see cref="Error"/> defined in the domain.
/// Keeps the user-facing copy and error code in one place (the domain) instead of
/// re-inventing them on each <c>RuleFor(...)</c>.
/// </summary>
public static class FluentValidationExtensions
{
    public static IRuleBuilderOptions<T, TProperty> WithDomainError<T, TProperty>(
        this IRuleBuilderOptions<T, TProperty> rule, Error error)
        => rule.WithErrorCode(error.Code).WithMessage(error.Message);
}
