using Application.Abstractions;
using Application.Data;
using Application.Features.Games.Dtos;
using Domain.Errors;
using Domain.Models.Game;
using Domain.Models.Static.Data;
using Domain.Models.User;
using Domain.Results;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Games;

/// <summary>
/// One game, as seen by <paramref name="UserId"/> — only games they own or sit in are
/// visible, the same set the list shows (see <see cref="GameQueries.InvolvingUser"/>).
/// </summary>
public sealed record GetGameQuery(Guid GameId, Guid UserId) : IQuery<GetGameResponse>;

/// <param name="ExtraBoardId">
/// Which lettered board was the extra one. Null on games recorded before it was asked for.
/// </param>
/// <param name="CustomLayoutName">
/// The saved layout the island came from, when the player set one up out of their library.
/// Null for published layouts, one-off hand-built islands, and layouts deleted since.
/// </param>
/// <param name="IslandLayoutJson">
/// The game's own copy of the arrangement played, for hand-built islands — the only picture
/// there is of one, since no thumbnail ships for a shape the player invented. Null when the
/// island was a published layout, which has one.
/// </param>
public sealed record GetGameResponse(
    Guid Id,
    DateTimeOffset StartedAt,
    string IslandSetupId,
    string? CustomLayoutName,
    string? IslandLayoutJson,
    int Difficulty,
    int DifficultyModifier,
    bool ExtraBoard,
    string? ExtraBoardId,
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
        // Owned types load with the game — no Includes needed (see GameQueries).
        var game = await db.Games
            .InvolvingUser(new UserId(request.UserId))
            .FirstOrDefaultAsync(g => g.Id == new GameId(request.GameId), cancellationToken);

        if (game is null)
            return Result.Failure<GetGameResponse>(DomainErrors.Game.NotFound);

        var setup = GameData.IslandSetups.FirstOrDefault(s => s.Id == game.IslandSetupId);
        var extraBoard = setup is not null && setup.BoardCount > game.Players.Count;
        var thematicMaps = setup?.IsThematic ?? false;

        // The saved layout's name, when the island came from the player's library. The game has
        // its own copy of the geometry, so a layout deleted since simply resolves to no name.
        string? customLayoutName = null;
        if (game.CustomLayoutId is { } layoutId)
        {
            customLayoutName = await db.CustomIslandLayouts
                .Where(l => l.Id == layoutId)
                .Select(l => l.Name.Value)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var response = new GetGameResponse(
            game.Id.Value,
            game.StartedAt,
            game.IslandSetupId.Value,
            customLayoutName,
            game.IslandLayout?.Value,
            game.Difficulty.Value,
            game.DifficultyModifier.Value,
            extraBoard,
            game.ExtraBoard?.Value,
            thematicMaps,
            game.Note?.Value,
            game.OwnerId.Value,
            game.Result is not null,
            game.Result?.ToResponse(),
            game.ScenarioId?.Value,
            game.Players.Select(p => p.ToResponse()).ToList(),
            game.PlayedAdversaries.Select(a => a.ToResponse()).ToList());

        return response;
    }
}
