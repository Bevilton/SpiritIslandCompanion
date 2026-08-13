using Application.Abstractions;
using Application.Behaviour;
using Application.Data;
using Application.Features.Games.Dtos;
using Domain.Errors;
using Domain.Models.Game;
using Domain.Models.User;
using Domain.Results;
using FluentValidation;

namespace Application.Features.Games;

/// <summary>
/// Creates a fully completed game with setup and result in one shot.
/// Difficulty and score are calculated server-side.
/// </summary>
/// <param name="ExtraBoardId">
/// Which lettered board is the extra one. Required when <paramref name="ExtraBoard"/> is set, and
/// must be left null when it isn't — deliberately not optional, so a caller has to say.
/// </param>
/// <param name="IslandLayoutJson">
/// Where the boards sat, required when <paramref name="IslandSetupId"/> is a hand-built one.
/// </param>
/// <param name="SavedLayoutId">The caller's saved layout this arrangement came from, if any.</param>
public sealed record CreateGameCommand(
    Guid OwnerId,
    DateTimeOffset StartedAt,
    string IslandSetupId,
    bool ExtraBoard,
    string? ExtraBoardId,
    bool ThematicMaps,
    int DifficultyModifier,
    List<GamePlayerDto> Players,
    List<GameAdversaryDto> Adversaries,
    string? ScenarioId,
    GameResultDto Result,
    string? Note,
    string? IslandLayoutJson = null,
    Guid? SavedLayoutId = null) : ICommand, IGameSetupCommand;

internal sealed class CreateGameValidator : AbstractValidator<CreateGameCommand>
{
    public CreateGameValidator()
    {
        this.AddGameSetupRules();
        RuleFor(x => x.Result).NotNull().WithDomainError(DomainErrors.Game.ResultRequired);
        RuleFor(x => x.Result).SetValidator(new GameResultDtoValidator())
            .When(x => x.Result is not null);
    }
}

internal sealed class CreateGameHandler(IAppDbContext db) : ICommandHandler<CreateGameCommand>
{
    public async Task<Result> Handle(CreateGameCommand request, CancellationToken cancellationToken)
    {
        var ownerId = new UserId(request.OwnerId);

        var setupResult = await GameFactory.BuildSetupAsync(db, request, ownerId, cancellationToken);
        if (setupResult.IsFailure)
            return Result.Failure(setupResult.Error);
        var setup = setupResult.Value;

        var gameResultOrError = GameFactory.BuildResult(request.Result, setup.Difficulty, setup.Players.Count);
        if (gameResultOrError.IsFailure)
            return Result.Failure(gameResultOrError.Error);

        var game = Game.Create(
            new GameId(Guid.NewGuid()),
            request.StartedAt,
            setup.Island,
            setup.Players,
            setup.Adversaries,
            setup.Scenario,
            setup.Difficulty,
            setup.Modifier,
            gameResultOrError.Value,
            setup.Note,
            ownerId);

        db.Games.Add(game);
        return Result.Success();
    }
}
