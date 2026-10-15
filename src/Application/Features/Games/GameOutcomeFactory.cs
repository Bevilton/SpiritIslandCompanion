using Application.Features.Games.Dtos;
using Domain.Models.Game;
using Domain.Results;

namespace Application.Features.Games;

/// <summary>
/// The reported outcome of a game as the domain takes it, built from the numbers the caller
/// submitted. The score is not among them: the game works it out when it takes the outcome,
/// from its own difficulty and table size. Shared by the command that writes a finished game
/// and the one that finishes a draft.
/// </summary>
internal static class GameOutcomeFactory
{
    public static Result<GameOutcome> Build(GameResultDto dto)
    {
        var cards = CardsCount.Create(dto.Cards);
        if (cards.IsFailure)
            return Result.Failure<GameOutcome>(cards.Error);

        var blight = BlightCount.Create(dto.Blight);
        if (blight.IsFailure)
            return Result.Failure<GameOutcome>(blight.Error);

        var dahan = DahanCount.Create(dto.Dahan);
        if (dahan.IsFailure)
            return Result.Failure<GameOutcome>(dahan.Error);

        var scoreModifier = ScoreModifier.Create(dto.ScoreModifier);
        if (scoreModifier.IsFailure)
            return Result.Failure<GameOutcome>(scoreModifier.Error);

        return GameOutcome.Create(
            dto.Win, dto.Duration, cards.Value, dto.TerrorLevel, blight.Value, dahan.Value, scoreModifier.Value);
    }
}
