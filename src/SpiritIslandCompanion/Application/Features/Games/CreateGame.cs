using Application.Abstractions;
using Application.Data;
using Application.Features.Games.Dtos;
using Domain.Models.Game;
using Domain.Models.Static;
using Domain.Models.User;
using Domain.Results;
using Domain.Services;
using FluentValidation;

namespace Application.Features.Games;

/// <summary>
/// Creates a fully completed game with setup and result in one shot.
/// Score is calculated server-side.
/// </summary>
public sealed record CreateGameCommand(
    Guid OwnerId,
    DateTimeOffset StartedAt,
    string IslandSetupId,
    int Difficulty,
    List<GamePlayerDto> Players,
    List<GameAdversaryDto> Adversaries,
    string? ScenarioId,
    GameResultDto Result,
    string? Note) : ICommand;

internal sealed class CreateGameValidator : AbstractValidator<CreateGameCommand>
{
    public CreateGameValidator()
    {
        RuleFor(x => x.OwnerId).NotEmpty();
        RuleFor(x => x.IslandSetupId).NotEmpty();
        RuleFor(x => x.Players).NotEmpty();
        RuleFor(x => x.Result).NotNull();
        RuleForEach(x => x.Players).ChildRules(p =>
        {
            p.RuleFor(x => x.SpiritId).NotEmpty();
            p.RuleFor(x => x.BoardId).NotEmpty();
        });
    }
}

internal sealed class CreateGameHandler(IAppDbContext db) : ICommandHandler<CreateGameCommand>
{
    public async Task<Result> Handle(CreateGameCommand request, CancellationToken cancellationToken)
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

        var gameResultOrError = GameFactory.BuildResult(request.Result, difficultyResult.Value);
        if (gameResultOrError.IsFailure)
            return Result.Failure(gameResultOrError.Error);

        GameNote? note = null;
        if (request.Note is not null)
        {
            var noteResult = GameNote.Create(request.Note);
            if (noteResult.IsFailure) return Result.Failure(noteResult.Error);
            note = noteResult.Value;
        }

        var game = Game.Create(
            new GameId(Guid.NewGuid()),
            request.StartedAt,
            new IslandSetupId(request.IslandSetupId),
            players,
            adversaries,
            scenario,
            difficultyResult.Value,
            gameResultOrError.Value,
            note,
            new UserId(request.OwnerId));

        db.Games.Add(game);
        return Result.Success();
    }
}
