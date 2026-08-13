using Application.Abstractions;
using Application.Data;
using Domain.Errors;
using Domain.Models.Game;
using Domain.Results;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Games;

/// <summary>Only the game's owner can delete it — for anyone else it isn't found.</summary>
public sealed record DeleteGameCommand(Guid GameId, Guid UserId) : ICommand;

internal sealed class DeleteGameHandler(IAppDbContext db) : ICommandHandler<DeleteGameCommand>
{
    public async Task<Result> Handle(DeleteGameCommand request, CancellationToken cancellationToken)
    {
        var game = await db.Games
            .FirstOrDefaultAsync(
                g => g.Id == new GameId(request.GameId) && g.OwnerId.Value == request.UserId,
                cancellationToken);

        if (game is null)
            return Result.Failure(DomainErrors.Game.NotFound);

        db.Games.Remove(game);
        return Result.Success();
    }
}
