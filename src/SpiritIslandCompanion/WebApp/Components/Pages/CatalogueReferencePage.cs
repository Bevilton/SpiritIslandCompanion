using Application.Abstractions;
using Application.Features.Statistics;
using Domain.Models.Static;
using MediatR;
using Microsoft.AspNetCore.Components;
using WebApp.Components.Shared.Games;

namespace WebApp.Components.Pages;

/// <summary>
/// The signed-in half of a reference page. Each catalogue has two routes — a public one that shows
/// the catalogue and an authenticated one that also shows what the player has done with it — and
/// the authenticated four were otherwise the same six lines four times over: resolve the user, load
/// the history, project it to a per-item record.
/// <para>
/// A page supplies only <see cref="Project"/>, naming which slice of the history its catalogue
/// wants. The projection is done once on load rather than per render: it walks every recorded seat,
/// and the page re-renders on every keystroke in its filter box.
/// </para>
/// </summary>
public abstract class CatalogueReferencePage : ComponentBase
{
    [Inject] protected IMediator Mediator { get; set; } = default!;
    [Inject] protected ICurrentUserService CurrentUser { get; set; } = default!;

    [CascadingParameter(Name = "OwnedExpansions")]
    protected IReadOnlyList<ExpansionId>? OwnedExpansions { get; set; }

    /// <summary>
    /// The player's record per catalogue item, or null when there is no history to show — which is
    /// what tells the view to leave the play-based orderings and the never-played filter out.
    /// </summary>
    protected IReadOnlyDictionary<string, SetupInsights.PlayRecord>? Records { get; private set; }

    /// <summary>
    /// The loaded history itself, for the detail modals — their "your record" profile needs the
    /// full games, not the per-item projection the cards show.
    /// </summary>
    protected IReadOnlyList<SetupGameFact>? Facts { get; private set; }

    /// <summary>Which slice of the history this catalogue is measured by.</summary>
    protected abstract IReadOnlyDictionary<string, SetupInsights.PlayRecord> Project(
        IReadOnlyList<SetupGameFact> facts);

    protected override async Task OnInitializedAsync()
    {
        if (CurrentUser.UserId is not { } userId) return;

        var result = await Mediator.Send(new GetSetupFactsQuery(userId));
        if (result.IsSuccess && result.Value.Count > 0)
        {
            Facts = result.Value;
            Records = Project(result.Value);
        }
    }
}
