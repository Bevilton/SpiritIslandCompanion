using Application.Abstractions;
using Application.Data;
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
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.CreatedByUserId).NotEmpty();
    }
}

internal sealed class CreatePlayerHandler(IAppDbContext db) : ICommandHandler<CreatePlayerCommand>
{
    public Task<Result> Handle(CreatePlayerCommand request, CancellationToken cancellationToken)
    {
        var player = Player.Create(
            new PlayerId(Guid.NewGuid()),
            PlayerName.Create(request.Name),
            new UserId(request.CreatedByUserId));

        db.Players.Add(player);
        return Task.FromResult(Result.Success());
    }
}
