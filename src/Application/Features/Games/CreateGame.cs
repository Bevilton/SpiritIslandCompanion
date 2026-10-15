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
/// Writes a game down: its setup, and its outcome if it has one yet. Without a
/// <paramref name="Result"/> the game is a draft, finished later through
/// <see cref="CompleteGameCommand"/>; a draft is held to the same rules as a finished game.
/// Difficulty and score are computed server-side.
/// </summary>
/// <param name="IslandSetupId">
/// The island the game was played on. Whether that is the thematic island, with the difficulty
/// bonus that carries, is the catalogue's word on this id — not a separate flag the caller
/// could set at odds with it.
/// </param>
/// <param name="ExtraBoardId">
/// Which lettered board is the extra one. Required when <paramref name="ExtraBoard"/> is set, and
/// must be left null when it isn't — deliberately not optional, so a caller has to say.
/// </param>
/// <param name="Result">How the game ended, or null for a game still in progress.</param>
/// <param name="IslandLayoutJson">
/// Where the boards sat, required when <paramref name="IslandSetupId"/> is a hand-built one.
/// </param>
/// <param name="SavedLayoutId">The caller's saved layout this arrangement came from, if any.</param>
public sealed record CreateGameCommand(
    Guid UserId,
    DateTimeOffset StartedAt,
    string IslandSetupId,
    bool ExtraBoard,
    string? ExtraBoardId,
    int DifficultyModifier,
    List<GamePlayerDto> Players,
    List<GameAdversaryDto> Adversaries,
    string? ScenarioId,
    GameResultDto? Result,
    string? Note,
    string? IslandLayoutJson = null,
    Guid? SavedLayoutId = null) : ICommand;

/// <summary>
/// The field-bound rules: what has to be there and within range, checked where a failure
/// can be pinned to a field. Everything about the table itself — whether the ids exist,
/// whether the boards and the island agree — is the domain's, in <see cref="GameSetup"/>.
/// </summary>
internal sealed class CreateGameValidator : AbstractValidator<CreateGameCommand>
{
    public CreateGameValidator()
    {
        RuleFor(x => x.IslandSetupId).NotEmpty().WithDomainError(DomainErrors.Game.IslandSetupRequired);
        RuleFor(x => x.Players).NotEmpty().WithDomainError(DomainErrors.Game.PlayersRequired);
        RuleForEach(x => x.Players).ChildRules(p =>
        {
            p.RuleFor(x => x.SpiritId).NotEmpty().WithDomainError(DomainErrors.Game.SpiritRequired);
            p.RuleFor(x => x.BoardId).NotEmpty().WithDomainError(DomainErrors.Game.BoardRequired);
            // Exactly one: a seat is an account's or a local player's, and a seat that named
            // both would leave the choice to whoever built it, silently.
            p.RuleFor(x => x).Must(x => x.UserId.HasValue ^ x.PlayerId.HasValue)
                .WithDomainError(DomainErrors.Game.AssigneeRequired)
                .OverridePropertyName("AssignedTo");
        });
        RuleForEach(x => x.Adversaries).ChildRules(a =>
        {
            a.RuleFor(x => x.AdversaryId).NotEmpty().WithDomainError(DomainErrors.Game.UnknownAdversary);
            a.RuleFor(x => x.Level).InclusiveBetween(0, GameRestrictions.MaximumAdversaryLevel)
                .WithDomainError(DomainErrors.Game.InvalidAdversaryLevel);
        });
        RuleFor(x => x.ExtraBoardId).NotEmpty().WithDomainError(DomainErrors.Game.ExtraBoardRequired)
            .When(x => x.ExtraBoard);
        RuleFor(x => x.ExtraBoardId).Empty().WithDomainError(DomainErrors.Game.ExtraBoardNotUsed)
            .When(x => !x.ExtraBoard);
        RuleFor(x => x.DifficultyModifier)
            .InclusiveBetween(GameRestrictions.MinimumDifficultyModifier, GameRestrictions.MaximumDifficultyModifier)
            .WithDomainError(DomainErrors.Game.InvalidDifficultyModifier);
        RuleFor(x => x.Note!).MaximumLength(GameRestrictions.MaximumNoteLength)
            .WithDomainError(DomainErrors.Game.NoteTooLong)
            .When(x => x.Note is not null);
        RuleFor(x => x.Result!).SetValidator(new GameResultDtoValidator())
            .When(x => x.Result is not null);
    }
}

internal sealed class CreateGameHandler(IAppDbContext db, GameSetupFactory setupFactory) : ICommandHandler<CreateGameCommand>
{
    public async Task<Result> Handle(CreateGameCommand request, CancellationToken cancellationToken)
    {
        var ownerId = new UserId(request.UserId);

        var note = GameNote.CreateOptional(request.Note);
        if (note.IsFailure)
            return Result.Failure(note.Error);

        GameOutcome? outcome = null;
        if (request.Result is not null)
        {
            var reported = GameOutcomeFactory.Build(request.Result);
            if (reported.IsFailure)
                return Result.Failure(reported.Error);
            outcome = reported.Value;
        }

        var setup = await setupFactory.BuildAsync(request, ownerId, cancellationToken);
        if (setup.IsFailure)
            return Result.Failure(setup.Error);

        db.Games.Add(Game.Create(
            new GameId(Guid.NewGuid()), request.StartedAt, ownerId, setup.Value, note.Value, outcome));

        return Result.Success();
    }
}
