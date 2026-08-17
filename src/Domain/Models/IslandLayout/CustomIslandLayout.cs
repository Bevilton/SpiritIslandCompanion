using Domain.Errors;
using Domain.Models.User;
using Domain.Primitives;
using Domain.Results;

namespace Domain.Models.IslandLayout;

/// <summary>
/// A board arrangement the player built by hand and named so they can set it up again in
/// a later game — their own entry alongside the published layouts in
/// <see cref="Static.Data.IslandSetups"/>.
/// <para>
/// <see cref="BoardCount"/> is counted off the arrangement and fixed at creation: a shape of
/// four boards is only offered for a four-board table, since the geometry says nothing about
/// how to add or remove one. Games record their own copy of the geometry, so renaming or
/// reshaping a layout here never rewrites history.
/// </para>
/// </summary>
public class CustomIslandLayout : AggregateRoot<CustomIslandLayoutId>
{
    public UserId OwnerId { get; private init; }
    public IslandLayoutName Name { get; private set; }
    public int BoardCount { get; private init; }
    public IslandLayoutGeometry Geometry { get; private set; }
    public DateTimeOffset CreatedAt { get; private init; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private CustomIslandLayout(
        CustomIslandLayoutId id,
        UserId ownerId,
        IslandLayoutName name,
        IslandLayoutGeometry geometry,
        DateTimeOffset createdAt)
        : base(id)
    {
        OwnerId = ownerId;
        Name = name;
        // Counted off the arrangement rather than taken from the caller: the two could then
        // disagree, and a layout filed under the wrong size is one that silently refuses to
        // load. Stored as its own column so the library can be ordered and filtered in SQL.
        BoardCount = geometry.BoardCount;
        Geometry = geometry;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public static CustomIslandLayout Create(
        CustomIslandLayoutId id,
        UserId ownerId,
        IslandLayoutName name,
        IslandLayoutGeometry geometry,
        DateTimeOffset createdAt)
        => new(id, ownerId, name, geometry, createdAt);

    /// <summary>
    /// Renames the layout and replaces its shape. A layout is built for a fixed number of
    /// boards, so a reshape that covers a different number is a different layout — it is
    /// rejected rather than silently changing what this one is.
    /// </summary>
    public Result Update(IslandLayoutName name, IslandLayoutGeometry geometry, DateTimeOffset updatedAt)
    {
        if (geometry.BoardCount != BoardCount)
            return Result.Failure(DomainErrors.IslandLayout.BoardCountMismatch);

        Name = name;
        Geometry = geometry;
        UpdatedAt = updatedAt;
        return Result.Success();
    }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    /// <summary>
    /// Empty constructor required for EF Core.
    /// </summary>
    [Obsolete("Empty constructor required for EF Core.")]
    private CustomIslandLayout() { }
#pragma warning restore
}
