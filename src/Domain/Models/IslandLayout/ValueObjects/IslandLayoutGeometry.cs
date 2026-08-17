using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.Errors;
using Domain.Models.Game;
using Domain.Primitives;
using Domain.Results;

namespace Domain.Models.IslandLayout;

/// <summary>
/// Where every board sits on the island, and the seams they are joined along.
/// <para>
/// The stored form is the island playground's own JSON, kept <em>verbatim</em> in
/// <see cref="Value"/>: the format belongs to <c>wwwroot/js/island-playground.js</c>, so a
/// field this side doesn't know about has to survive the round trip untouched, and the six
/// decimal places the playground chose have to come back exactly as they went out.
/// </para>
/// <para>
/// What this type validates is only what the domain can actually own — that the payload is an
/// arrangement at all, of a playable number of boards, whose seams refer to boards that exist.
/// Whether those seams are geometrically possible is decided by the board's traced outline in
/// the browser and is not checked here.
/// </para>
/// </summary>
public record IslandLayoutGeometry : ValueObject
{
    /// <summary>
    /// Generous: six boards' placements and their bonds at six decimal places come to
    /// roughly 1 kB, and the cap is only here to stop something unbounded being stored.
    /// </summary>
    public const int MaxLength = 20_000;

    /// <summary>The payload exactly as the playground produced it.</summary>
    public string Value { get; private init; }

    private IslandLayoutGeometry(string value)
    {
        Value = value;
    }

    /// <summary>
    /// How many boards the arrangement describes — the one thing about the shape the domain
    /// reasons over, since a layout only fits a table of exactly that size. Read off the
    /// payload rather than stored beside it, so the two can never disagree; a computed
    /// property also keeps it out of the mapped columns of this owned type.
    /// </summary>
    public int BoardCount => Parse(Value)?.Boards?.Count ?? 0;

    public static Result<IslandLayoutGeometry> Create(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return Result.Failure<IslandLayoutGeometry>(DomainErrors.IslandLayout.GeometryRequired);
        if (json.Length > MaxLength)
            return Result.Failure<IslandLayoutGeometry>(DomainErrors.IslandLayout.GeometryTooLong);

        if (Parse(json) is not { Boards.Count: > 0 } arrangement)
            return Result.Failure<IslandLayoutGeometry>(DomainErrors.IslandLayout.GeometryMalformed);
        if (arrangement.Boards.Count > GameRestrictions.MaximumBoards)
            return Result.Failure<IslandLayoutGeometry>(DomainErrors.IslandLayout.InvalidBoardCount);

        // A seam between boards that aren't there describes no island. This is the payload's
        // own consistency, not board geometry, so it is this side's to check.
        var boards = arrangement.Boards.Count;
        if ((arrangement.Bonds ?? []).Any(b =>
                b.A < 0 || b.A >= boards || b.B < 0 || b.B >= boards || b.A == b.B
                || b.AEdge < 0 || b.BEdge < 0))
            return Result.Failure<IslandLayoutGeometry>(DomainErrors.IslandLayout.GeometryMalformed);

        return new IslandLayoutGeometry(json);
    }

    /// <summary>The payload's two lists — the parse target, not part of the model.</summary>
    private sealed record Arrangement(
        [property: JsonPropertyName("boards")] IReadOnlyList<Placement>? Boards,
        [property: JsonPropertyName("bonds")] IReadOnlyList<Bond>? Bonds);

    /// <summary>Where one board sits: its rotation in degrees, and the position of its centre.</summary>
    private sealed record Placement(
        [property: JsonPropertyName("rot")] double Rotation,
        [property: JsonPropertyName("x")] double X,
        [property: JsonPropertyName("y")] double Y);

    /// <summary>
    /// A seam two boards are clicked together along: board <paramref name="A"/>'s edge
    /// <paramref name="AEdge"/> meets board <paramref name="B"/>'s edge <paramref name="BEdge"/>,
    /// <paramref name="Slide"/> along it. Which edges can meet at all, and at which slides, is
    /// measured off the board's traced outline in the browser — not something this side knows.
    /// </summary>
    private sealed record Bond(
        [property: JsonPropertyName("a")] int A,
        [property: JsonPropertyName("ae")] int AEdge,
        [property: JsonPropertyName("b")] int B,
        [property: JsonPropertyName("be")] int BEdge,
        [property: JsonPropertyName("s")] double Slide);

    /// <summary>
    /// Null for anything that isn't an arrangement. Malformed payloads can only come from a
    /// caller that bypassed <see cref="Create"/>, so reading one yields an empty island
    /// rather than throwing out of a property getter.
    /// </summary>
    private static Arrangement? Parse(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<Arrangement>(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
