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
/// Adds result and scoring data to a previously drafted game. Score is calculated
/// server-side. Only the game's owner can complete it — for anyone else it isn't found.
/// </summary>
public sealed record CompleteGameCommand(
    Guid GameId,
    Guid UserId,
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
        // Owned types load with the game — no Includes needed (see GameQueries).
        var game = await db.Games
            .FirstOrDefaultAsync(
                g => g.Id == new GameId(request.GameId) && g.OwnerId.Value == request.UserId,
                cancellationToken);

        if (game is null)
            return Result.Failure(DomainErrors.Game.NotFound);

        if (game.Result is not null)
            return Result.Failure(DomainErrors.Game.AlreadyCompleted);

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

        game.Complete(gameResultOrError.Value, note);

        return Result.Success();
    }
}
