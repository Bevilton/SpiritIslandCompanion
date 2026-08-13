using Domain.Models.IslandLayout;
using Domain.Models.Static;
using Domain.Primitives;

namespace Domain.Models.Game;

/// <summary>
/// The island a game was played on, as one argument.
/// <para>
/// The parts belong together and are meaningless apart: <paramref name="SetupId"/> names
/// the layout, <paramref name="Layout"/> carries the arrangement when that layout is a
/// hand-built one, <paramref name="SavedLayoutId"/> points at the library entry it came
/// from, and <paramref name="ExtraBoard"/> names the board that is on the island without a
/// spirit on it. Passing them separately through <see cref="Game"/>'s factories invited
/// setting one and forgetting another.
/// </para>
/// </summary>
/// <param name="ExtraBoard">
/// The lettered board nobody plays, when the game adds one to raise the difficulty; null
/// otherwise. It is a physical board like any other — which letter it is decides that
/// region's terrain, so it is part of the setup, not a bare "+1 board".
/// </param>
public record IslandChoice(
    IslandSetupId SetupId,
    IslandLayoutGeometry? Layout = null,
    CustomIslandLayoutId? SavedLayoutId = null,
    BoardId? ExtraBoard = null) : ValueObject
{
    /// <summary>One of the published layouts — nothing to remember beyond its id.</summary>
    public static IslandChoice Published(IslandSetupId setupId, BoardId? extraBoard = null) =>
        new(setupId, ExtraBoard: extraBoard);
}
