using Application.Abstractions;
using Application.Data;
using Application.Features.Games.Dtos;
using Domain.Models.Game;
using Domain.Models.User;
using Domain.Results;
using FluentValidation;

namespace Application.Features.Games;

/// <summary>
/// Creates a game with only the setup information (players, spirits, boards, adversaries, scenario).
/// No result or scoring data. Use <see cref="CompleteGameCommand"/> to add the result later.
/// Difficulty is calculated server-side.
/// </summary>
/// <param name="ExtraBoardId">
/// Which lettered board is the extra one. Required when <paramref name="ExtraBoard"/> is set, and
/// must be left null when it isn't — deliberately not optional, so a caller has to say.
/// </param>
/// <param name="IslandLayoutJson">
/// Where the boards sat, required when <paramref name="IslandSetupId"/> is a hand-built one.
/// </param>
/// <param name="SavedLayoutId">The caller's saved layout this arrangement came from, if any.</param>
public sealed record DraftGameCommand(
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
    string? Note,
    string? IslandLayoutJson = null,
    Guid? SavedLayoutId = null) : ICommand, IGameSetupCommand;

internal sealed class DraftGameValidator : AbstractValidator<DraftGameCommand>
{
    public DraftGameValidator() => this.AddGameSetupRules();
}

internal sealed class DraftGameHandler(IAppDbContext db) : ICommandHandler<DraftGameCommand>
{
    public async Task<Result> Handle(DraftGameCommand request, CancellationToken cancellationToken)
    {
        var ownerId = new UserId(request.OwnerId);

        var setupResult = await GameFactory.BuildSetupAsync(db, request, ownerId, cancellationToken);
        if (setupResult.IsFailure)
            return Result.Failure(setupResult.Error);
        var setup = setupResult.Value;

        var game = Game.StartNew(
            new GameId(Guid.NewGuid()),
            request.StartedAt,
            setup.Island,
            setup.Players,
            setup.Adversaries,
            setup.Scenario,
            setup.Difficulty,
            setup.Modifier,
            setup.Note,
            ownerId);

        db.Games.Add(game);
        return Result.Success();
    }
}
