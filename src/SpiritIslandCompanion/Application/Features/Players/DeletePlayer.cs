using Application.Abstractions;
using Application.Data;
using Domain.Errors;
using Domain.Models.Player;
using Domain.Models.PlayerMerge;
using Domain.Results;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Players;

/// <summary>Deletes one of your own local players. Seats they hold are left naming nobody.</summary>
public sealed record DeletePlayerCommand(Guid PlayerId, Guid UserId) : ICommand;

internal sealed class DeletePlayerHandler(IAppDbContext db) : ICommandHandler<DeletePlayerCommand>
{
    public async Task<Result> Handle(DeletePlayerCommand request, CancellationToken cancellationToken)
    {
        var player = await db.Players
            .FirstOrDefaultAsync(p => p.Id == new PlayerId(request.PlayerId), cancellationToken);

        if (player is null)
            return Result.Failure(DomainErrors.Player.NotFound);
        if (player.CreatedBy.Value != request.UserId)
            return Result.Failure(DomainErrors.Player.NotYours);

        // A request to hand this player's seats to an account outlives the player it names.
        var merges = await db.PlayerMergeRequests
            .Where(m => m.PlayerId.Value == request.PlayerId && m.Status == PlayerMergeStatus.Pending)
            .ToListAsync(cancellationToken);

        foreach (var merge in merges)
            db.PlayerMergeRequests.Remove(merge);

        db.Players.Remove(player);
        return Result.Success();
    }
}
