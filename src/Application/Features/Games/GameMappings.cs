using Application.Features.Games.Dtos;
using Domain.Models.Game;

namespace Application.Features.Games;

/// <summary>
/// The response shapes of a game's parts, projected in one place so the readers that show
/// them — the game itself, the list, a merge's preview — cannot describe them differently.
/// </summary>
internal static class GameMappings
{
    public static GameAdversaryResponse ToResponse(this PlayedAdversary adversary) =>
        new(adversary.AdversaryId.Value, adversary.Level.Value);

    public static GamePlayerResponse ToResponse(this GamePlayer seat) =>
        new(seat.SpiritId.Value, seat.AspectId?.Value, seat.StartingBoard.Value, seat.UserId?.Value, seat.PlayerId?.Value);

    public static GameResultResponse ToResponse(this GameResult result) =>
        new(
            result.Outcome.Win,
            result.Outcome.Duration,
            result.Outcome.Cards.Value,
            result.Outcome.TerrorLevel,
            result.Outcome.Blight.Value,
            result.Outcome.Dahan.Value,
            result.Score.Value,
            result.Outcome.ScoreModifier.Value);
}
