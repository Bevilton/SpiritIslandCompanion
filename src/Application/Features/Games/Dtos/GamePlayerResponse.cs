namespace Application.Features.Games.Dtos;

public sealed record GamePlayerResponse(
    string SpiritId,
    string? AspectId,
    string BoardId,
    Guid? UserId,
    Guid? PlayerId);
