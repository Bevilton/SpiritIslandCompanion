using Application.Features.Statistics;
using Microsoft.AspNetCore.Components;
using WebApp.Components.Shared.Games;

namespace WebApp.Components.Shared;

/// <summary>
/// What every reference sheet is handed about the record it shows. The five sheets — spirit,
/// adversary, board, scenario, island layout — differ entirely in what they are sheets *of*, and
/// not at all in this: each took the same history, the same scope over it, and the same
/// permission to link out of it, and each spelled the five parameters out with the same
/// documentation. Three of them had already been reduced to "see AdversaryDetailModal", which is
/// what a shared base says properly.
/// <para>
/// A sheet is opened imperatively — the host holds a <c>@ref</c> and calls its <c>Show</c> — so
/// these are all set before the sheet is up, and nothing here changes while it is.
/// </para>
/// </summary>
public abstract class DetailSheetBase : ComponentBase
{
    /// <summary>
    /// A sheet stacks above the pickers that open it (z-50), so a record can be read over the
    /// grid of choices it was opened from. Spelled out on all five <c>ModalShell</c>s, along with
    /// the width below — the two numbers that make them one family of dialogs rather than five.
    /// </summary>
    protected const string SheetZIndex = "z-[60]";

    /// <summary>Wider than a dialog's default: a sheet's body is record tables.</summary>
    protected const string SheetWidth = "md:max-w-4xl";

    /// <summary>
    /// Personal game history. When there is any, the sheet profiles the record with this subject;
    /// empty or null leaves the profile out, which is what makes the same component a plain
    /// reference sheet on the public routes.
    /// </summary>
    [Parameter] public IReadOnlyList<SetupGameFact>? Facts { get; set; }

    /// <summary>
    /// Whether the record may link out to the games behind it. Off by default, and never set by
    /// the setup pickers: following a link from there would leave a half-built game behind.
    /// </summary>
    [Parameter] public bool LinkGames { get; set; }

    /// <summary>
    /// Narrows the record to one person. For a subject that sits in a seat (a spirit, a board)
    /// that means keeping their seats; for one the whole table shares (a foe, a scenario, an
    /// island) there are no seats of its own, so it drops the games they were not at — which is
    /// what <see cref="SetupProfiles.GamesWith"/> does and how the statistics page scopes its own
    /// numbers. Set by the hosts that have a scope of their own — the statistics page's player
    /// selector, the seat being filled on the setup screen — and left off by the reference pages,
    /// where a sheet is always the whole account's history. The sheet states which of the two it
    /// is; there is no control inside it to change.
    /// </summary>
    [Parameter] public Func<SetupFactSeat, bool>? SeatFilter { get; set; }

    /// <summary>Whose record it is when it isn't the reader's own, e.g. "Anna".</summary>
    [Parameter] public string? SubjectName { get; set; }

    /// <summary>
    /// Set by a host that loads no history at all — the public reference pages. Without it an
    /// empty <see cref="Facts"/> means "you have played nothing here", which is a claim about a
    /// reader the page knows nothing about; a signed-in player browsing the public route has a
    /// record, just not one that page went and fetched.
    /// <para>
    /// Only the two sheets that report an empty record in words — the board's and the island
    /// layout's, the two subjects with no reference text of their own — have anything to do with
    /// it. The rest simply leave the profile out, which says nothing either way.
    /// </para>
    /// </summary>
    [Parameter] public bool ReferenceOnly { get; set; }

    /// <summary>The same scope as a games-list <c>who</c> key, so the links out stay narrowed.</summary>
    [Parameter] public string? ScopeWho { get; set; }

    /// <summary>The profile the sheet renders, or null where there is no history to report.</summary>
    protected SetupProfiles.Profile? Profile { get; set; }

    /// <summary>
    /// Whether the numbers are one person's rather than the whole account's — what
    /// <see cref="SetupProfileSection"/> is told, and what every heading on the sheet follows.
    /// </summary>
    protected bool Scoped => SeatFilter is not null;

    /// <summary>
    /// Whose record the sheet's own strips should say they are reporting — "Anna's", "your", or
    /// null for the whole account's. The record strips inside a sheet (the adversary's level
    /// ladder, the spirit's variant row) head themselves with the same owner the profile below
    /// them counts, or the two disagree about the same numbers; both had spelled the decision out.
    /// </summary>
    protected string? RecordOwner => Copy.Owner(SubjectName, Scoped);

    /// <summary>
    /// The history narrowed to the person the sheet is about, for the subjects the whole table
    /// shares. Null once there is no history at all; empty is a real answer and stays one, so a
    /// sheet can report a blank record as theirs rather than vanishing.
    /// </summary>
    protected IReadOnlyList<SetupGameFact>? ScopedFacts() =>
        Facts is { Count: > 0 } ? SetupProfiles.GamesWith(Facts, SeatFilter) : null;
}
