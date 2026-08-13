using Domain.Results;

namespace WebApp.Validation;

/// <summary>
/// Everything a form has to say back to the user after it acts, in one place: the field-bound
/// validation errors, the one general message that has no field to sit on, and how many times
/// the user has tried.
/// <para>
/// The two-tier model this routes into is described in <see cref="FormErrors"/>: shape errors
/// go to <c>FormErrorSummary</c> and <c>FieldError</c>, anything else to <c>Alert</c>.
/// </para>
/// <para>
/// <see cref="Attempt"/> is the part that is easy to miss and the reason this is a class rather
/// than three loose fields. A second submit that fails exactly like the first changes nothing
/// on the page, so it reads as if the button did nothing at all; the counter gives
/// <c>FormErrorSummary</c> something to notice, so it can pull the page back to the errors and
/// say them again. It only moves on failure — correcting a field must not yank the page away
/// from what the user is doing.
/// </para>
/// </summary>
public sealed class FormFeedback
{
    /// <summary>Field-bound validation errors, for the summary and the per-field messages.</summary>
    public FormErrors Errors { get; private set; } = FormErrors.Empty;

    /// <summary>The general message — a success confirmation, or a failure with no field.</summary>
    public string? Message { get; private set; }

    /// <summary>Which <c>Alert</c> style <see cref="Message"/> wants: "success", "danger", …</summary>
    public string MessageType { get; private set; } = "danger";

    /// <summary>Failed submits so far. See the note on this type.</summary>
    public int Attempt { get; private set; }

    /// <summary>
    /// Routes a command result: validation failures to <see cref="Errors"/>, any other failure
    /// to <see cref="Message"/>, success to the confirmation. Returns whether it succeeded, so
    /// callers can read as <c>if (form.Apply(result, "Saved.")) …</c>.
    /// </summary>
    public bool Apply(Result result, string? successMessage = null, string successType = "success")
    {
        Errors = FormErrors.FromResult(result);

        if (result.IsSuccess)
        {
            Message = successMessage;
            MessageType = successType;
            return true;
        }

        Attempt++;
        // A failure is either field-bound or general, never both: the summary already lists the
        // field errors, and repeating one of them in the alert says it twice.
        Message = Errors.Any ? null : result.Error.Message;
        MessageType = "danger";
        return false;
    }

    /// <summary>
    /// The user has just filled in something the form complained about — drop that error so the
    /// summary shrinks as they work through it instead of standing still until the next submit.
    /// Deliberately does not touch <see cref="Attempt"/>.
    /// </summary>
    public void Resolve(params string[] propertyPaths)
    {
        if (!Errors.Any) return;
        Errors = Errors.Without(propertyPaths);
    }

    /// <summary>Back to saying nothing — e.g. when a dialog is opened afresh.</summary>
    public void Clear()
    {
        Errors = FormErrors.Empty;
        Message = null;
    }
}
