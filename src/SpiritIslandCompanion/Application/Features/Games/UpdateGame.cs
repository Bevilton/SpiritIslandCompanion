using Application.Abstractions;
using Application.Data;
using Application.Features.Games.Dtos;
using Domain.Models.Game;
using Domain.Models.Static;
using Domain.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Games;

/// <summary>
/// Updates a game's setup information. Can also update or set the result.
/// Difficulty and score are calculated server-side.
/// </summary>
public sealed record UpdateGameCommand(
    Guid GameId,
    DateTimeOffset StartedAt,
    string IslandSetupId,
    bool ExtraBoard,
    bool ThematicMaps,
    int DifficultyModifier,
    List<GamePlayerDto> Players,
    List<GameAdversaryDto> Adversaries,
    string? ScenarioId,
    GameResultDto? Result,
    string? Note) : ICommand;

internal sealed class UpdateGameValidator : AbstractValidator<UpdateGameCommand>
{
    public UpdateGameValidator()
    {
        RuleFor(x => x.GameId).NotEmpty();
        RuleFor(x => x.IslandSetupId).NotEmpty();
        RuleFor(x => x.Players).NotEmpty();
    }
}

internal sealed class UpdateGameHandler(IAppDbContext db) : ICommandHandler<UpdateGameCommand>
{
    public async Task<Result> Handle(UpdateGameCommand request, CancellationToken cancellationToken)
    {
        var game = await db.Games
            .Include(g => g.Players)
            .Include(g => g.PlayedAdversaries)
            .Include(g => g.Scenario)
            .Include(g => g.Result)
            .FirstOrDefaultAsync(g => g.Id == new GameId(request.GameId), cancellationToken);

        if (game is null)
            return Result.Failure(Error.NotFound("Game.NotFound", "Game not found."));

        var friendshipCheck = await GameFactory.ValidatePlayerFriendships(game.OwnerId, request.Players, db, cancellationToken);
        if (friendshipCheck.IsFailure)
            return friendshipCheck;

        var setupCheck = GameFactory.ValidateIslandSetup(request.IslandSetupId, request.Players.Count, request.ExtraBoard, request.ThematicMaps);
        if (setupCheck.IsFailure)
            return setupCheck;

        var difficultyResult = GameFactory.ComputeDifficulty(
            request.ScenarioId, request.Adversaries, request.ExtraBoard, request.ThematicMaps, request.DifficultyModifier);
        if (difficultyResult.IsFailure)
            return Result.Failure(difficultyResult.Error);

        var modifierResult = Domain.Models.Game.DifficultyModifier.Create(request.DifficultyModifier);
        if (modifierResult.IsFailure)
            return Result.Failure(modifierResult.Error);

        var players = GameFactory.BuildPlayers(request.Players);
        var adversaries = GameFactory.BuildAdversaries(request.Adversaries);
        var scenario = GameFactory.BuildScenario(request.ScenarioId);

        GameResult? gameResult = null;
        if (request.Result is not null)
        {
            var resultOrError = GameFactory.BuildResult(request.Result, difficultyResult.Value, players.Count);
            if (resultOrError.IsFailure) return Result.Failure(resultOrError.Error);
            gameResult = resultOrError.Value;
        }

        GameNote? note = null;
        if (request.Note is not null)
        {
            var noteResult = GameNote.Create(request.Note);
            if (noteResult.IsFailure) return Result.Failure(noteResult.Error);
            note = noteResult.Value;
        }

        game.Update(
            request.StartedAt,
            new IslandSetupId(request.IslandSetupId),
            players,
            adversaries,
            scenario,
            difficultyResult.Value,
            modifierResult.Value,
            gameResult,
            note);

        return Result.Success();
    }
}
