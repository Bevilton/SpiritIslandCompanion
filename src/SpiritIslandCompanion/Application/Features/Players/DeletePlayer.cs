using Application.Abstractions;
using Application.Data;
using Domain.Models.Player;
using Domain.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Players;

public sealed record DeletePlayerCommand(Guid PlayerId) : ICommand;

internal sealed class DeletePlayerValidator : AbstractValidator<DeletePlayerCommand>
{
    public DeletePlayerValidator()
    {
        RuleFor(x => x.PlayerId).NotEmpty();
    }
}

internal sealed class DeletePlayerHandler(IAppDbContext db) : ICommandHandler<DeletePlayerCommand>
{
    public async Task<Result> Handle(DeletePlayerCommand request, CancellationToken cancellationToken)
    {
        var player = await db.Players
            .FirstOrDefaultAsync(p => p.Id == new PlayerId(request.PlayerId), cancellationToken);

        if (player is null)
            return Result.Failure(Error.NotFound("Player.NotFound", "Player not found."));

        db.Players.Remove(player);
        return Result.Success();
    }
}
