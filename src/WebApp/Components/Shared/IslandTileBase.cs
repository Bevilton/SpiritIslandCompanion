using Application.Features.IslandLayouts;
using Domain.Models.Static;
using Microsoft.AspNetCore.Components;

namespace WebApp.Components.Shared;

/// <summary>
/// What an island tile is a tile *of*. Two components draw one — <c>IslandLayoutTile</c>, the
/// index entry that opens a layout's record sheet, and <c>IslandChoiceTile</c>, the one the setup
/// form picks an island with — and both take the same pair: a published layout, or a shape from
/// the player's own library. Neither is a mode of the other, so the pair is asked for the same way
/// in both and answered here once.
/// <para>
/// It was answered twice. <c>Name</c> and <c>BoardCount</c> were byte-identical in the two files,
/// their parameter documentation was the same sentence written out twice, and the board count was
/// then spelled <c>Copy.Count(BoardCount, "board")</c> in one and held as a property in the other
/// — so the day <see cref="IslandLayoutResponse"/> grows a third shape, one of the two tiles
/// learns about it and the other does not.
/// </para>
/// <para>
/// The parallel to <see cref="DetailSheetBase"/> and <see cref="Views.CatalogueViewBase"/> is
/// deliberate: what a component is handed about its subject belongs to the family, and what it
/// does with it belongs to the component. The tints, the layout and the tooltip are each tile's
/// own, and stay there.
/// </para>
/// </summary>
public abstract class IslandTileBase : ComponentBase
{
    /// <summary>A published layout, whose picture ships with the app.</summary>
    [Parameter] public IslandSetup? Setup { get; set; }

    /// <summary>A shape from the player's library, drawn from the geometry it was kept with.
    /// Ignored when <see cref="Setup"/> is given — a tile is one island or the other.</summary>
    [Parameter] public IslandLayoutResponse? Saved { get; set; }

    /// <summary>The player's own shape rather than a published one — which is what the accent
    /// tint and the "yours" mark on the index tile say.</summary>
    protected bool IsSaved => Setup is null && Saved is not null;

    /// <summary>One of the published thematic islands, as opposed to a standard arrangement.</summary>
    protected bool IsThematic => Setup?.IsThematic == true;

    /// <summary>
    /// The tint the kind of island wears — <c>.tint-saved</c> for the player's own shape,
    /// <c>.tint-thematic</c> for a published thematic island, and none for a standard layout,
    /// which takes its surface's own colours. The record sheet's header band picks the same class
    /// by the same rule (see <c>IslandLayoutDetailModal</c>), so the recipe lives in site.css and
    /// only the name is chosen here.
    /// </summary>
    protected string? KindTint => IsSaved ? "tint-saved" : IsThematic ? "tint-thematic" : null;

    /// <summary>What the island is called. The fallback is for a saved shape kept without a name,
    /// which the library allows.</summary>
    protected string Name => Setup?.Name ?? Saved?.Name ?? "Island layout";

    /// <summary>How many boards it is cut into.</summary>
    protected int BoardCount => Setup?.NumberOfPlayers ?? Saved?.BoardCount ?? 0;

    /// <summary>The board count in words — "4 boards" — for the tooltip and the line under the
    /// name.</summary>
    protected string Boards => Copy.Count(BoardCount, "board");
}
