namespace WebApp.Components.Shared.Games;

/// <summary>
/// The state ladder a record tile wears — the adversary level tiles and the spirit sheet's
/// aspect tiles are the same control at two sizes, and each carried its own copy of the
/// ladder. Shape (radius, padding, alignment) stays with the caller; this is only which
/// state shows.
/// </summary>
public static class TileChrome
{
    /// <param name="current">Outranks the played tint — which tile is chosen, or focused, is
    /// the one thing the host has to be able to see at a glance.</param>
    public static string For(bool played, bool interactive, bool current) =>
        "border transition-colors "
        + (current ? "border-accent-500 bg-accent-50 ring-1 ring-accent-500/30"
            : played ? "border-ink-300 bg-white"
            : "border-ink-200 bg-ink-50/60")
        + (interactive ? " hover:border-accent-400 hover:bg-accent-50" : "");

    /// <summary>
    /// The setup form's current pick — the board strip's tile and the island tile mark "this is
    /// the one" identically, and each had its own copy of the spelling. Deliberately softer than
    /// <c>.pick-card.is-selected</c>: a picker chooses once and closes, while these tiles stay on
    /// screen and are re-read, so theirs is a wash and a quiet ring rather than the hard one.
    /// </summary>
    public const string Selected = "border-accent-500 bg-accent-50/70 ring-2 ring-accent-500/30";

    /// <summary>
    /// The same tiles at rest — a board not chosen, an island not on the table — and how they
    /// answer the pointer. The pair the two strips share as they share <see cref="Selected"/>;
    /// each had its own spelling of it.
    /// </summary>
    public const string Resting = "border-ink-200 bg-white hover:border-ink-300 hover:bg-ink-50";
}
