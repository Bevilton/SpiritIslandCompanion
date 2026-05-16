using Application.Abstractions;
using Application.Behaviour;
using Application.Data;
using Domain.Errors;
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
        RuleFor(x => x.Name)
            .NotEmpty().WithDomainError(DomainErrors.Player.NameRequired)
            .MaximumLength(PlayerName.MaxLength).WithDomainError(DomainErrors.Player.NameTooLong);
    }
}

internal sealed class UpdatePlayerHandler(IAppDbContext db) : ICommandHandler<UpdatePlayerCommand>
{
    public async Task<Result> Handle(UpdatePlayerCommand request, CancellationToken cancellationToken)
    {
        var nameResult = PlayerName.Create(request.Name);
        if (nameResult.IsFailure)
            return Result.Failure(nameResult.Error);

        var player = await db.Players
            .FirstOrDefaultAsync(p => p.Id == new PlayerId(request.PlayerId), cancellationToken);

        if (player is null)
            return Result.Failure(Error.NotFound("Player.NotFound", "Player not found."));

        player.Rename(nameResult.Value);
        return Result.Success();
    }
}
