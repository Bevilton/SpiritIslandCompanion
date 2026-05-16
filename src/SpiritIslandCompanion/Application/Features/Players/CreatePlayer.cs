using Application.Abstractions;
using Application.Behaviour;
using Application.Data;
using Domain.Errors;
using Domain.Models.Player;
using Domain.Models.User;
using Domain.Results;
using FluentValidation;

namespace Application.Features.Players;

public sealed record CreatePlayerCommand(string Name, Guid CreatedByUserId) : ICommand;

internal sealed class CreatePlayerValidator : AbstractValidator<CreatePlayerCommand>
{
    public CreatePlayerValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithDomainError(DomainErrors.Player.NameRequired)
            .MaximumLength(PlayerName.MaxLength).WithDomainError(DomainErrors.Player.NameTooLong);
    }
}

internal sealed class CreatePlayerHandler(IAppDbContext db) : ICommandHandler<CreatePlayerCommand>
{
    public Task<Result> Handle(CreatePlayerCommand request, CancellationToken cancellationToken)
    {
        var nameResult = PlayerName.Create(request.Name);
        if (nameResult.IsFailure)
            return Task.FromResult(Result.Failure(nameResult.Error));

        var player = Player.Create(
            new PlayerId(Guid.NewGuid()),
            nameResult.Value,
            new UserId(request.CreatedByUserId));

        db.Players.Add(player);
        return Task.FromResult(Result.Success());
    }
}
