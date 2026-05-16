using Application.Abstractions;
using Application.Behaviour;
using Application.Data;
using Application.Features.Games.Dtos;
using Domain.Errors;
using Domain.Models.Game;
using Domain.Models.Static;
using Domain.Models.User;
using Domain.Results;
using FluentValidation;

namespace Application.Features.Games;

/// <summary>
/// Creates a game with only the setup information (players, spirits, boards, adversaries, scenario).
/// No result or scoring data. Use <see cref="CompleteGameCommand"/> to add the result later.
/// Difficulty is calculated server-side.
/// </summary>
public sealed record DraftGameCommand(
    Guid OwnerId,
    DateTimeOffset StartedAt,
    string IslandSetupId,
    bool ExtraBoard,
    bool ThematicMaps,
    int DifficultyModifier,
    List<GamePlayerDto> Players,
    List<GameAdversaryDto> Adversaries,
    string? ScenarioId,
    string? Note) : ICommand;

internal sealed class DraftGameValidator : AbstractValidator<DraftGameCommand>
{
    public DraftGameValidator()
    {
        RuleFor(x => x.IslandSetupId).NotEmpty().WithDomainError(DomainErrors.Game.IslandSetupRequired);
        RuleFor(x => x.Players).NotEmpty().WithDomainError(DomainErrors.Game.PlayersRequired);
        RuleForEach(x => x.Players).ChildRules(p =>
        {
            p.RuleFor(x => x.SpiritId).NotEmpty().WithDomainError(DomainErrors.Game.SpiritRequired);
            p.RuleFor(x => x.BoardId).NotEmpty().WithDomainError(DomainErrors.Game.BoardRequired);
            p.RuleFor(x => x).Must(x => x.UserId.HasValue || x.PlayerId.HasValue)
                .WithDomainError(DomainErrors.Game.AssigneeRequired)
                .OverridePropertyName("AssignedTo");
        });
        RuleFor(x => x.DifficultyModifier)
            .InclusiveBetween(GameRestrictions.DifficultyModifierMin, GameRestrictions.DifficultyModifierMax)
            .WithDomainError(DomainErrors.Game.InvalidDifficultyModifier);
        RuleFor(x => x.Note!).MaximumLength(GameRestrictions.NoteLength)
            .WithDomainError(DomainErrors.Game.NoteTooLong)
            .When(x => x.Note is not null);
    }
}

internal sealed class DraftGameHandler(IAppDbContext db) : ICommandHandler<DraftGameCommand>
{
    public async Task<Result> Handle(DraftGameCommand request, CancellationToken cancellationToken)
    {
        var ownerId = new UserId(request.OwnerId);

        var catalogCheck = GameFactory.ValidateCatalogReferences(request.Players, request.Adversaries, request.ScenarioId);
        if (catalogCheck.IsFailure)
            return catalogCheck;

        var duplicatesCheck = GameFactory.ValidateNoDuplicates(request.Players, request.Adversaries);
        if (duplicatesCheck.IsFailure)
            return duplicatesCheck;

        var friendshipCheck = await GameFactory.ValidatePlayerFriendships(ownerId, request.Players, db, cancellationToken);
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
            modifierResult.Value,
            note,
            new UserId(request.OwnerId));

        db.Games.Add(game);
        return Result.Success();
    }
}
