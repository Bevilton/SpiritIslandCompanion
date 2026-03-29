using Application.Abstractions;
using Application.Data;
using Domain.Models.Player;
using Domain.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Players;

public sealed record UpdatePlayerCommand(Guid PlayerId, string Name) : ICommand;

internal sealed class UpdatePlayerValidator : AbstractValidator<UpdatePlayerCommand>
{
    public UpdatePlayerValidator()
    {
        RuleFor(x => x.PlayerId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}

internal sealed class UpdatePlayerHandler(IAppDbContext db) : ICommandHandler<UpdatePlayerCommand>
{
    public async Task<Result> Handle(UpdatePlayerCommand request, CancellationToken cancellationToken)
    {
        var player = await db.Players
            .FirstOrDefaultAsync(p => p.Id == new PlayerId(request.PlayerId), cancellationToken);

        if (player is null)
            return Result.Failure(Error.NotFound("Player.NotFound", "Player not found."));

        player.Rename(PlayerName.Create(request.Name));
        return Result.Success();
    }
}
