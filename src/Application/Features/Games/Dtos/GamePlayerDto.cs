namespace Application.Features.Games.Dtos;

public sealed record GamePlayerDto(
    string SpiritId,
    string? AspectId,
    string BoardId,
    Guid? UserId,
    Guid? PlayerId);
