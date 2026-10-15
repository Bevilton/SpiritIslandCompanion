using Application.Abstractions;
using Application.Data;
using Application.Features.Games;
using Application.Features.PlayerMerges;
using Domain.Errors;
using Domain.Models.Player;
using Domain.Models.User;
using Domain.Results;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Players;

/// <summary>
/// Deletes one of your own local players. The seats they held stay in their games but name
/// nobody from here on: the evening was played, and a chair is not emptied by forgetting who
/// sat in it.
/// </summary>
public sealed record DeletePlayerCommand(Guid PlayerId, Guid UserId) : ICommand;

internal sealed class DeletePlayerHandler(IAppDbContext db) : ICommandHandler<DeletePlayerCommand>
{
    public async Task<Result> Handle(DeletePlayerCommand request, CancellationToken cancellationToken)
    {
        var player = await db.Players
            .FirstOrDefaultAsync(p => p.Id == new PlayerId(request.PlayerId), cancellationToken);

        if (player is null)
            return Result.Failure(DomainErrors.Player.NotFound);
        if (player.CreatedBy != new UserId(request.UserId))
            return Result.Failure(DomainErrors.Player.NotYours);

        var games = await db.Games
            .SeatingLocalPlayer(player.Id)
            .ToListAsync(cancellationToken);

        foreach (var game in games)
            game.UnseatLocalPlayer(player.Id);

        // Every request naming this player goes with them — one still waiting and one that came
        // back declined alike: a request about a guest who no longer exists has nothing left to
        // say, and left behind it would surface on the People page as a card about nobody.
        var merges = await db.PlayerMergeRequests
            .About(player.Id)
            .ToListAsync(cancellationToken);

        db.PlayerMergeRequests.RemoveRange(merges);
        db.Players.Remove(player);
        return Result.Success();
    }
}
