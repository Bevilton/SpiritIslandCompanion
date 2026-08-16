using Application.Abstractions;
using Application.Behaviour;
using Application.Data;
using Domain.Errors;
using Domain.Models.Player;
using Domain.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Players;

/// <summary>Renames one of your own local players.</summary>
public sealed record UpdatePlayerCommand(Guid PlayerId, string Name, Guid UserId) : ICommand;

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
            return Result.Failure(DomainErrors.Player.NotFound);
        // A local player belongs to the account that wrote them down — the same bar
        // DeletePlayer sets, since renaming somebody else's guest is no less of a change.
        if (player.CreatedBy.Value != request.UserId)
            return Result.Failure(DomainErrors.Player.NotYours);

        player.Rename(nameResult.Value);
        return Result.Success();
    }
}
