using Application.Abstractions;
using Application.Data;
using Application.Features.Games.Dtos;
using Domain.Models.Game;
using Domain.Models.Static.Data;
using Domain.Results;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Games;

public sealed record GetGameQuery(Guid GameId) : IQuery<GetGameResponse>;

public sealed record GetGameResponse(
    Guid Id,
    DateTimeOffset StartedAt,
    string IslandSetupId,
    int Difficulty,
    int DifficultyModifier,
    bool ExtraBoard,
    bool ThematicMaps,
    string? Note,
    Guid OwnerId,
    bool IsCompleted,
    GameResultResponse? Result,
    string? ScenarioId,
    List<GamePlayerResponse> Players,
    List<GameAdversaryResponse> Adversaries);

internal sealed class GetGameHandler(IAppDbContext db) : IQueryHandler<GetGameQuery, GetGameResponse>
{
    public async Task<Result<GetGameResponse>> Handle(GetGameQuery request, CancellationToken cancellationToken)
    {
        var game = await db.Games
            .AsNoTracking()
            .Include(g => g.Players)
            .Include(g => g.PlayedAdversaries)
            .Include(g => g.Scenario)
            .Include(g => g.Result)
            .FirstOrDefaultAsync(g => g.Id == new GameId(request.GameId), cancellationToken);

        if (game is null)
            return Result.Failure<GetGameResponse>(Error.NotFound("Game.NotFound", "Game not found."));

        var setup = GameData.IslandSetups.FirstOrDefault(s => s.Id.Value == game.IslandSetupId.Value);
        var extraBoard = setup is not null && setup.NumberOfPlayers > game.Players.Count;
        var thematicMaps = setup?.IsThematic ?? false;

        var response = new GetGameResponse(
            game.Id.Value,
            game.StartedAt,
            game.IslandSetupId.Value,
            game.Difficulty.Value,
            game.DifficultyModifier.Value,
            extraBoard,
            thematicMaps,
            game.Note?.Value,
            game.OwnerId.Value,
            game.Result is not null,
            game.Result is not null
                ? new GameResultResponse(
                    game.Result.Win,
                    game.Result.Duration,
                    game.Result.Cards.Value,
                    game.Result.TerrorLevel,
                    game.Result.Blight.Value,
                    game.Result.Dahan.Value,
                    game.Result.Score.Value,
                    game.Result.ScoreModifier.Value)
                : null,
            game.Scenario?.ScenarioId.Value,
            game.Players.Select(p => new GamePlayerResponse(
                p.SpiritId.Value,
                p.AspectId?.Value,
                p.StartingBoard.Value,
                p.UserId?.Value,
                p.PlayerId?.Value)).ToList(),
            game.PlayedAdversaries.Select(a => new GameAdversaryResponse(
                a.AdversaryId.Value,
                a.Level.Value)).ToList());

        return response;
    }
}
