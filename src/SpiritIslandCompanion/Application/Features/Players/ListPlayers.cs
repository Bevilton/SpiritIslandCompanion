using Application.Abstractions;
using Application.Data;
using Domain.Models.User;
using Domain.Results;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Players;

public sealed record ListPlayersQuery(Guid UserId) : IQuery<List<ListPlayersResponse>>;

public sealed record ListPlayersResponse(Guid Id, string Name);

internal sealed class ListPlayersHandler(IAppDbContext db) : IQueryHandler<ListPlayersQuery, List<ListPlayersResponse>>
{
    public async Task<Result<List<ListPlayersResponse>>> Handle(ListPlayersQuery request, CancellationToken cancellationToken)
    {
        var players = await db.Players
            .AsNoTracking()
            .Where(p => p.CreatedBy == new UserId(request.UserId))
            .OrderBy(p => p.Name.Value)
            .Select(p => new ListPlayersResponse(p.Id.Value, p.Name.Value))
            .ToListAsync(cancellationToken);

        return players;
    }
}
