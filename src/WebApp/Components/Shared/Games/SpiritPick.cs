using Domain.Models.Static;

namespace WebApp.Components.Shared.Games;

/// <summary>
/// A spirit chosen for a seat together with the aspect it will be played as — null for the
/// spirit as printed. The two travel as one value because they are one decision: an aspect
/// rewrites enough of a spirit to have its own record, so the pickers offer it alongside the
/// spirit rather than only after one has been settled on.
/// </summary>
public sealed record SpiritPick(Spirit Spirit, Aspect? Aspect)
{
    /// <summary>The aspect's catalogue id, or null for the spirit as printed.</summary>
    public string? AspectId => Aspect?.Id.Value;
}
