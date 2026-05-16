using Domain.Results;

namespace WebApp.Validation;

/// <summary>
/// UI-side wrapper around <see cref="ValidationError"/>s produced by the application
/// layer's FluentValidation validators. Lets forms render an aggregate list and
/// look up errors per FluentValidation property path (e.g. <c>Players[0].SpiritId</c>).
/// <para>
/// Only "shape" validation (multi-error, field-bound) flows through here. General
/// server failures (auth, missing IDs, business-rule violations) come back as a
/// regular <see cref="Result"/> with a single domain <see cref="Error"/> and are
/// rendered by the page's <c>Alert</c> instead.
/// </para>
/// </summary>
public sealed class FormErrors
{
    public static FormErrors Empty { get; } = new(Array.Empty<ValidationError>());

    private readonly Dictionary<string, List<string>> _byProperty;
    private readonly IReadOnlyList<string> _all;

    public FormErrors(IEnumerable<ValidationError> errors)
    {
        var list = errors.ToList();

        _all = list
            .Select(e => e.Message)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        _byProperty = list
            .GroupBy(e => e.Property, StringComparer.Ordinal)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.Message).Distinct(StringComparer.Ordinal).ToList(),
                StringComparer.Ordinal);
    }

    public bool Any => _all.Count > 0;
    public IReadOnlyList<string> All => _all;

    public string? For(string propertyPath) =>
        _byProperty.TryGetValue(propertyPath, out var msgs) && msgs.Count > 0 ? msgs[0] : null;

    public IReadOnlyList<string> AllFor(string propertyPath) =>
        _byProperty.TryGetValue(propertyPath, out var msgs) ? msgs : Array.Empty<string>();

    public bool HasFor(string propertyPath) =>
        _byProperty.TryGetValue(propertyPath, out var msgs) && msgs.Count > 0;

    /// <summary>
    /// Returns a populated <see cref="FormErrors"/> if the result carries validation
    /// failures, otherwise <see cref="Empty"/>.
    /// </summary>
    public static FormErrors FromResult(Result result) =>
        result is IValidationResult vr ? new FormErrors(vr.Errors) : Empty;
}
