using Application.Abstractions;
using Application.Behaviour;
using Application.Data;
using Application.Features.Games.Dtos;
using Domain.Errors;
using Domain.Models.Game;
using Domain.Models.User;
using Domain.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Games;

/// <summary>
/// Adds result and scoring data to a previously drafted game. Score is calculated
/// server-side. Only the game's owner can complete it — for anyone else it isn't found.
/// </summary>
/// <param name="Note">A new note for the game, or null to keep whatever the draft was saved with.</param>
public sealed record CompleteGameCommand(
    Guid GameId,
    Guid UserId,
    GameResultDto Result,
    string? Note) : ICommand;

internal sealed class CompleteGameValidator : AbstractValidator<CompleteGameCommand>
{
    public CompleteGameValidator()
    {
        RuleFor(x => x.Note!).MaximumLength(GameRestrictions.MaximumNoteLength)
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
        // The seats, adversaries and outcome are owned, so they load with the game (see GameQueries).
        var ownerId = new UserId(request.UserId);
        var game = await db.Games
            .FirstOrDefaultAsync(
                g => g.Id == new GameId(request.GameId) && g.OwnerId == ownerId,
                cancellationToken);

        if (game is null)
            return Result.Failure(DomainErrors.Game.NotFound);

        // Whether the game already has an outcome is the game's own rule (see Game.Complete). The
        // numbers below have already passed the validator, so it is the only answer left to give.
        var outcome = GameOutcomeFactory.Build(request.Result);
        if (outcome.IsFailure)
            return Result.Failure(outcome.Error);

        var note = GameNote.CreateOptional(request.Note);
        if (note.IsFailure)
            return Result.Failure(note.Error);

        return game.Complete(outcome.Value, note.Value);
    }
}
