using Domain.Models.Game;

namespace Application.Features.Games.Dtos;

public sealed record GameResultResponse(
    bool Win,
    TimeSpan Duration,
    int Cards,
    TerrorLevel TerrorLevel,
    int Blight,
    int Dahan,
    int Score,
    int ScoreModifier);
