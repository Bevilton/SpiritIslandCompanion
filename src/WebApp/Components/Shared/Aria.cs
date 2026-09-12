namespace WebApp.Components.Shared;

/// <summary>
/// ARIA state attributes that must be spelled as literal text. Handed a <c>bool</c>, Blazor
/// renders an attribute by presence — <c>aria-expanded</c> for true and nothing at all for false
/// — so a closed disclosure or an off switch reads to a screen reader as having no state. Nine
/// call sites carried the same ternary and the same explanation of it; this is that ternary,
/// once.
/// </summary>
public static class Aria
{
    /// <summary>"true" or "false" — for <c>aria-expanded</c>, <c>aria-pressed</c> and the like.</summary>
    public static string Flag(bool value) => value ? "true" : "false";
}
