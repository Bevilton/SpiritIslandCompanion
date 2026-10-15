using System.Reflection;
using Domain.Results;
using FluentValidation;
using MediatR;

namespace Application.Behaviour;

/// <summary>
/// Runs the request's validators before its handler and, when any rule fails, answers with
/// every failure at once — as a validation result, so a form can show them field by field.
/// </summary>
public sealed class ValidationBehaviour<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    /// <summary>
    /// How a failure is wrapped as this response type: <see cref="ValidationResult"/> for a
    /// bare <see cref="Result"/>, <see cref="ValidationResult{TValue}"/> for a
    /// <see cref="Result{TValue}"/>. Resolved once per closed type rather than by reflection
    /// on every failing request.
    /// </summary>
    private static readonly Func<ValidationError[], TResponse> Failure = ResolveFailureFactory();

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var context = new ValidationContext<TRequest>(request);
        var errors = new List<ValidationError>();

        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(context, cancellationToken);
            errors.AddRange(result.Errors.Select(f => new ValidationError(f.PropertyName, f.ErrorCode, f.ErrorMessage)));
        }

        return errors.Count > 0
            ? Failure(errors.Distinct().ToArray())
            : await next(cancellationToken);
    }

    private static Func<ValidationError[], TResponse> ResolveFailureFactory()
    {
        if (typeof(TResponse) == typeof(Result))
            return errors => (TResponse)(Result)ValidationResult.WithErrors(errors);

        // Result<TValue>: build the matching ValidationResult<TValue> through its own factory.
        var valueType = typeof(TResponse).GetGenericArguments()[0];
        var factory = typeof(ValidationResult<>)
            .MakeGenericType(valueType)
            .GetMethod(nameof(ValidationResult<>.WithErrors), BindingFlags.Public | BindingFlags.Static)!;

        return errors => (TResponse)factory.Invoke(null, [errors])!;
    }
}
