using System.Diagnostics.CodeAnalysis;
using Application.Features.Statistics;
using Domain.Models.Static;
using Microsoft.AspNetCore.Components;
using WebApp.Components.Shared.Games;
using WebApp.Components.Shared.Games.Filtering;
using WebApp.State;

namespace WebApp.Components.Shared.Views;

/// <summary>
/// What every reference view is handed. Each catalogue — spirits, adversaries, scenarios, boards
/// — is browsed through one view rendered on two routes: a public one that shows the catalogue,
/// and a signed-in one that also shows what the reader has done with it (see
/// <see cref="Pages.CatalogueReferencePage"/>, the other half of the same pairing). The four
/// views differ entirely in what they are catalogues *of*, and not at all in this: each took the
/// same three parameters and documented them the same way, four times over.
/// <para>
/// The parallel to <see cref="DetailSheetBase"/> is exact and deliberate — a view and the record
/// sheet it opens are the same catalogue at two depths, and are told the same things about the
/// reader's history.
/// </para>
/// </summary>
public abstract class CatalogueViewBase : ComponentBase
{
    /// <summary>
    /// The expansions on the reader's shelf, read from the same cascade every picker reads. Null
    /// — the public routes, whose shell cascades nothing, and the moment before the signed-in
    /// shell has loaded — shows the whole catalogue rather than none of it (see
    /// <see cref="OwnedCatalogue"/>). It was a parameter, and the signed-in pages existed partly
    /// to forward the very value the cascade already carried.
    /// </summary>
    [CascadingParameter(Name = CurrentUserState.OwnedExpansionsCascade)]
    public IReadOnlyList<ExpansionId>? OwnedExpansions { get; set; }

    /// <summary>
    /// The account's record per catalogue item — every game the reader has recorded, whoever sat
    /// at the table, since a reference page has no player selector and never narrows to one
    /// person. It unlocks the play-based orderings, the never-played filter and the figures on
    /// the cards; sorting by a number the cards don't show would leave the order looking
    /// arbitrary, which is why the three arrive together.
    /// <para>
    /// Null on the public route, which has nobody to have a record — and that null is what tells
    /// the view to leave all three out.
    /// </para>
    /// </summary>
    [Parameter] public IReadOnlyDictionary<string, SetupInsights.PlayRecord>? Records { get; set; }

    /// <summary>The history behind <see cref="Records"/>, for the detail sheets' record profile —
    /// they need the games themselves, not the per-item projection the cards show.</summary>
    [Parameter] public IReadOnlyList<SetupGameFact>? Facts { get; set; }

    /// <summary>
    /// Whether there is a record at all — what every record pill, play-based ordering and the
    /// detail sheets' <c>LinkGames</c> in a view gate on: the public route has no reader to have
    /// one and no games page to send them to. The attribute lets a view read
    /// <see cref="Records"/> inside the test without a second null check.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Records))]
    protected bool HasRecord => Records is not null;

    /// <summary>
    /// The view's filter bar state. Declared here because of what the base does with it below —
    /// each view builds its own (the catalogues ask different questions), but every one of them
    /// has to be handed the record before it can order or sift by play.
    /// </summary>
    protected abstract FilterState Filter { get; }

    /// <summary>
    /// Points <see cref="Filter"/> at the record on every parameter change. All four views opened
    /// with this exact line, which is four chances for a fifth to arrive without it and quietly
    /// offer play-based orderings over an empty history.
    /// <para>
    /// Sealed on purpose: two of the views have derived work of their own to do here, and the
    /// obvious shape — a virtual method each of them overrides — relies on every subclass
    /// remembering <c>base.OnParametersSet()</c>, where forgetting it breaks the filter silently.
    /// Sealing this and offering <see cref="OnRecordsChanged"/> instead makes that mistake a
    /// compile error rather than a bug nobody sees.
    /// </para>
    /// </summary>
    protected sealed override void OnParametersSet()
    {
        Filter.UseHistory(Records);
        OnRecordsChanged();
    }

    /// <summary>
    /// Whatever else a view derives from the parameters it was just handed — the per-aspect
    /// tally the spirit cards read, the layout index the boards page builds. Runs after the
    /// filter has the record, so it may rely on it.
    /// </summary>
    protected virtual void OnRecordsChanged()
    {
    }
}
