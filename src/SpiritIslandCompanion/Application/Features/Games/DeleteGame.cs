using Application.Abstractions;
using Application.Data;
using Domain.Models.Game;
using Domain.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Games;

public sealed record DeleteGameCommand(Guid GameId) : ICommand;

internal sealed class DeleteGameValidator : AbstractValidator<DeleteGameCommand>
{
    public DeleteGameValidator()
    {
        RuleFor(x => x.GameId).NotEmpty();
    }
}

internal sealed class DeleteGameHandler(IAppDbContext db) : ICommandHandler<DeleteGameCommand>
{
    public async Task<Result> Handle(DeleteGameCommand request, CancellationToken cancellationToken)
    {
        var game = await db.Games
            .FirstOrDefaultAsync(g => g.Id == new GameId(request.GameId), cancellationToken);

        if (game is null)
            return Result.Failure(Error.NotFound("Game.NotFound", "Game not found."));

        db.Games.Remove(game);
        return Result.Success();
    }
}
