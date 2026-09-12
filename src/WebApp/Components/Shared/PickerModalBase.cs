using Domain.Models.Static;
using Microsoft.AspNetCore.Components;
using WebApp.Components.Shared.Games;
using WebApp.State;

namespace WebApp.Components.Shared;

/// <summary>
/// The lifecycle every setup picker shares. A picker is opened imperatively — the game form
/// holds a @ref and calls its Show — snapshots what it needs from the form, and closes itself
/// on pick, backdrop or Escape. The four pickers each spelled this skeleton out, cascade
/// comment included; Show stays with each picker, since what is snapshotted is the part that
/// genuinely differs.
/// </summary>
public abstract class PickerModalBase : ComponentBase
{
    /// <summary>What the signed-in player owns, for the filter bar's expansion row. Null means
    /// no filtering — an anonymous visitor, or the moment before the cascade has loaded.</summary>
    [CascadingParameter(Name = CurrentUserState.OwnedExpansionsCascade)]
    public IReadOnlyList<ExpansionId>? OwnedExpansions { get; set; }

    /// <summary>Whether the dialog is up. The host owns nothing here — a picker closes itself.</summary>
    protected bool IsOpen { get; private set; }

    /// <summary>
    /// The setup this picker was opened for — the history behind each card's record and the
    /// detail sheets' profiles. Set by Show as a snapshot: the form behind the modal cannot
    /// change while it is up.
    /// </summary>
    protected SetupInsights.MatchupContext Context { get; set; } = SetupInsights.MatchupContext.Empty;

    /// <summary>
    /// The start of every Show(): adopt the setup the picker was opened for, before the snapshot
    /// that reads it. A picker opened with no context gets the empty one rather than null — the
    /// rule every card, pill and filter below then relies on, and it had been spelled out with
    /// its own <c>?? MatchupContext.Empty</c> in all four pickers.
    /// </summary>
    protected void BeginShow(SetupInsights.MatchupContext? context) =>
        Context = context ?? SetupInsights.MatchupContext.Empty;

    /// <summary>The end of every Show(): raise the dialog over the snapshot just taken.</summary>
    protected void Open()
    {
        IsOpen = true;
        StateHasChanged();
    }

    protected void Close() => IsOpen = false;
}
