using Application.Abstractions;
using Application.Data;
using Application.Features.Games.Dtos;
using Domain.Models.Game;
using Domain.Models.Static;
using Domain.Models.User;
using Domain.Results;
using FluentValidation;

namespace Application.Features.Games;

/// <summary>
/// Creates a game with only the setup information (players, spirits, boards, adversaries, scenario).
/// No result or scoring data. Use <see cref="CompleteGameCommand"/> to add the result later.
/// </summary>
public sealed record DraftGameCommand(
    Guid OwnerId,
    DateTimeOffset StartedAt,
    string IslandSetupId,
    int Difficulty,
    List<GamePlayerDto> Players,
    List<GameAdversaryDto> Adversaries,
    string? ScenarioId,
    string? Note) : ICommand;

internal sealed class DraftGameValidator : AbstractValidator<DraftGameCommand>
{
    public DraftGameValidator()
    {
        RuleFor(x => x.OwnerId).NotEmpty();
        RuleFor(x => x.IslandSetupId).NotEmpty();
        RuleFor(x => x.Players).NotEmpty();
        RuleForEach(x => x.Players).ChildRules(p =>
        {
            p.RuleFor(x => x.SpiritId).NotEmpty();
            p.RuleFor(x => x.BoardId).NotEmpty();
        });
    }
}

internal sealed class DraftGameHandler(IAppDbContext db) : ICommandHandler<DraftGameCommand>
{
    public async Task<Result> Handle(DraftGameCommand request, CancellationToken cancellationToken)
    {
        var ownerId = new UserId(request.OwnerId);

        var friendshipCheck = await GameFactory.ValidatePlayerFriendships(ownerId, request.Players, db, cancellationToken);
        if (friendshipCheck.IsFailure)
            return friendshipCheck;

        var difficultyResult = Difficulty.Create(request.Difficulty);
        if (difficultyResult.IsFailure)
            return Result.Failure(difficultyResult.Error);

        var players = GameFactory.BuildPlayers(request.Players);
        var adversaries = GameFactory.BuildAdversaries(request.Adversaries);
        var scenario = GameFactory.BuildScenario(request.ScenarioId);

        GameNote? note = null;
        if (request.Note is not null)
        {
            var noteResult = GameNote.Create(request.Note);
            if (noteResult.IsFailure) return Result.Failure(noteResult.Error);
            note = noteResult.Value;
        }

        var game = Game.StartNew(
            new GameId(Guid.NewGuid()),
            request.StartedAt,
            new IslandSetupId(request.IslandSetupId),
            players,
            adversaries,
            scenario,
            difficultyResult.Value,
            note,
            new UserId(request.OwnerId));

        db.Games.Add(game);
        return Result.Success();
    }
}
