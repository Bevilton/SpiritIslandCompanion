using Domain.Models.Game.Enums;

namespace Application.Features.Games.Dtos;

/// <summary>
/// DTO for submitting game result data. Score is excluded because it is always
/// calculated server-side based on the official scoring rules.
/// </summary>
public sealed record GameResultDto(
    bool Win,
    TimeSpan Duration,
    int Cards,
    TerrorLevel TerrorLevel,
    int Blight,
    int Dahan,
    int ScoreModifier);
