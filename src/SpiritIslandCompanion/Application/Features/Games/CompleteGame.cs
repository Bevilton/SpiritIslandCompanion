using Application.Abstractions;
using Application.Behaviour;
using Application.Data;
using Application.Features.Games.Dtos;
using Domain.Errors;
using Domain.Models.Game;
using Domain.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Games;

/// <summary>
/// Adds result and scoring data to a previously drafted game.
/// Score is calculated server-side.
/// </summary>
public sealed record CompleteGameCommand(
    Guid GameId,
    GameResultDto Result,
    string? Note) : ICommand;

internal sealed class CompleteGameValidator : AbstractValidator<CompleteGameCommand>
{
    public CompleteGameValidator()
    {
        RuleFor(x => x.Note!).MaximumLength(GameRestrictions.NoteLength)
            .WithDomainError(DomainErrors.Game.NoteTooLong)
            .When(x => x.Note is not null);
        RuleFor(x => x.Result).NotNull().WithDomainError(DomainErrors.Game.ResultRequired);
        RuleFor(x => x.Result).SetValidator(new GameResultDtoValidator())
            .When(x => x.Result is not null);
    }
}

internal sealed class CompleteGameHandler(IAppDbContext db) : ICommandHandler<CompleteGameCommand>
{
    public async Task<Result> Handle(CompleteGameCommand request, CancellationToken cancellationToken)
    {
        var game = await db.Games
            .Include(g => g.Players)
            .Include(g => g.PlayedAdversaries)
            .Include(g => g.Scenario)
            .Include(g => g.Result)
            .FirstOrDefaultAsync(g => g.Id == new GameId(request.GameId), cancellationToken);

        if (game is null)
            return Result.Failure(Error.NotFound("Game.NotFound", "Game not found."));

        if (game.Result is not null)
            return Result.Failure(Error.Conflict("Game.AlreadyCompleted", "Game already has a result."));

        var gameResultOrError = GameFactory.BuildResult(request.Result, game.Difficulty, game.Players.Count);
        if (gameResultOrError.IsFailure)
            return Result.Failure(gameResultOrError.Error);

        GameNote? note = game.Note;
        if (request.Note is not null)
        {
            var noteResult = GameNote.Create(request.Note);
            if (noteResult.IsFailure) return Result.Failure(noteResult.Error);
            note = noteResult.Value;
        }

        game.Update(
            game.StartedAt,
            game.IslandSetupId,
            game.Players.ToList(),
            game.PlayedAdversaries.ToList(),
            game.Scenario,
            game.Difficulty,
            game.DifficultyModifier,
            gameResultOrError.Value,
            note);

        return Result.Success();
    }
}
