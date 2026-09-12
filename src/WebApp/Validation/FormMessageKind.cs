namespace WebApp.Validation;

/// <summary>
/// How a <see cref="FormFeedback.Message"/> reads, and therefore which <c>Alert</c> it is dressed
/// as. Was a string — "success", "danger", "info", plus a "warn"/"warning" branch nothing ever
/// produced — spelled out in two files that had to agree by hand, and with the caller's spelling
/// unchecked: a typo fell through to the neutral tone rather than failing.
/// <para>
/// <see cref="Failure"/> is first so that it is also the default: a kind that was never set must
/// read as "it did not work", never as good news.
/// </para>
/// </summary>
public enum FormMessageKind
{
    /// <summary>It did not work.</summary>
    Failure,

    /// <summary>It worked, and that is good news. Jade.</summary>
    Success,

    /// <summary>It worked, and it is simply a fact — something declined, cancelled or removed.</summary>
    Info,
}
