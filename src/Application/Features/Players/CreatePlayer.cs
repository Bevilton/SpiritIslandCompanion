using Application.Abstractions;
using Application.Behaviour;
using Application.Data;
using Domain.Errors;
using Domain.Models.Player;
using Domain.Models.User;
using Domain.Results;
using FluentValidation;

namespace Application.Features.Players;

public sealed record CreatePlayerCommand(string Name, Guid UserId) : ICommand;

internal sealed class CreatePlayerValidator : AbstractValidator<CreatePlayerCommand>
{
    public CreatePlayerValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithDomainError(DomainErrors.Player.NameRequired)
            .MaximumTrimmedLength(PlayerName.MaxLength).WithDomainError(DomainErrors.Player.NameTooLong);
    }
}

internal sealed class CreatePlayerHandler(IAppDbContext db) : ICommandHandler<CreatePlayerCommand>
{
    public Task<Result> Handle(CreatePlayerCommand request, CancellationToken cancellationToken)
    {
        var name = PlayerName.Create(request.Name);
        if (name.IsFailure)
            return Task.FromResult(Result.Failure(name.Error));

        var player = Player.Create(
            new PlayerId(Guid.NewGuid()),
            name.Value,
            new UserId(request.UserId));

        db.Players.Add(player);
        return Task.FromResult(Result.Success());
    }
}
