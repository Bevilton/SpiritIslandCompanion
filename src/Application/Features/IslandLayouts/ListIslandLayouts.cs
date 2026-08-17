using Application.Abstractions;
using Application.Data;
using Domain.Results;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.IslandLayouts;

/// <summary>
/// The caller's saved layouts. The game-setup screen loads them all once and filters by
/// board count in memory, the same way it does with the rest of the setup data — the list
/// is a handful of rows per user.
/// </summary>
public sealed record ListIslandLayoutsQuery(Guid UserId) : IQuery<List<IslandLayoutResponse>>;

public sealed record IslandLayoutResponse(
    Guid Id,
    string Name,
    int BoardCount,
    string LayoutJson,
    DateTimeOffset UpdatedAt);

internal sealed class ListIslandLayoutsHandler(IAppDbContext db)
    : IQueryHandler<ListIslandLayoutsQuery, List<IslandLayoutResponse>>
{
    public async Task<Result<List<IslandLayoutResponse>>> Handle(
        ListIslandLayoutsQuery request, CancellationToken cancellationToken)
    {
        var layouts = await db.CustomIslandLayouts
            .AsNoTracking()
            .Where(l => l.OwnerId.Value == request.UserId)
            .OrderBy(l => l.BoardCount)
            .ThenBy(l => l.Name.Value)
            .Select(l => new IslandLayoutResponse(
                l.Id.Value,
                l.Name.Value,
                l.BoardCount,
                l.Geometry.Value,
                l.UpdatedAt))
            .ToListAsync(cancellationToken);

        return layouts;
    }
}
