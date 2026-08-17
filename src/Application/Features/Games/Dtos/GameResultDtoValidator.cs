using Application.Behaviour;
using Domain.Errors;
using Domain.Models.Game;
using FluentValidation;

namespace Application.Features.Games.Dtos;

/// <summary>
/// Shared FluentValidation rules for <see cref="GameResultDto"/>. Reused by every
/// command that accepts a game result so the field-level checks (range, enum, …)
/// stay in one place.
/// </summary>
internal sealed class GameResultDtoValidator : AbstractValidator<GameResultDto>
{
    public GameResultDtoValidator()
    {
        RuleFor(x => x.Duration).GreaterThanOrEqualTo(TimeSpan.Zero)
            .WithDomainError(DomainErrors.Game.DurationNegative);
        RuleFor(x => x.TerrorLevel).IsInEnum()
            .WithDomainError(DomainErrors.Game.InvalidTerrorLevel);
        RuleFor(x => x.Cards).InclusiveBetween(0, GameRestrictions.MaximumCardsCount)
            .WithDomainError(DomainErrors.Game.InvalidCardCount);
        RuleFor(x => x.Blight).InclusiveBetween(0, GameRestrictions.MaximumBlightCount)
            .WithDomainError(DomainErrors.Game.InvalidBlightCount);
        RuleFor(x => x.Dahan).InclusiveBetween(0, GameRestrictions.MaximumDahanCount)
            .WithDomainError(DomainErrors.Game.InvalidDahanCount);
        RuleFor(x => x.ScoreModifier).InclusiveBetween(GameRestrictions.MinimumScoreModifier, GameRestrictions.MaximumScoreModifier)
            .WithDomainError(DomainErrors.Game.InvalidScoreModifier);
    }
}
