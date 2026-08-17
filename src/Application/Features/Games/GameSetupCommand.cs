using Application.Behaviour;
using Application.Features.Games.Dtos;
using Domain.Errors;
using Domain.Models.Game;
using FluentValidation;

namespace Application.Features.Games;

/// <summary>
/// The setup half shared by every command that writes a whole game. Create and Draft differ
/// only in what surrounds the setup — a result or not — so its field list, the field-bound
/// validation rules and the build pipeline (<c>GameFactory.BuildSetupAsync</c>) exist once
/// instead of once per command.
/// </summary>
public interface IGameSetupCommand
{
    string IslandSetupId { get; }
    bool ExtraBoard { get; }
    string? ExtraBoardId { get; }
    bool ThematicMaps { get; }
    int DifficultyModifier { get; }
    List<GamePlayerDto> Players { get; }
    List<GameAdversaryDto> Adversaries { get; }
    string? ScenarioId { get; }
    string? Note { get; }
    string? IslandLayoutJson { get; }
    Guid? SavedLayoutId { get; }
}

internal static class GameSetupRules
{
    /// <summary>The field-bound (Type 1) rules every game-writing command shares.</summary>
    public static void AddGameSetupRules<T>(this AbstractValidator<T> validator)
        where T : IGameSetupCommand
    {
        validator.RuleFor(x => x.IslandSetupId).NotEmpty().WithDomainError(DomainErrors.Game.IslandSetupRequired);
        validator.RuleFor(x => x.Players).NotEmpty().WithDomainError(DomainErrors.Game.PlayersRequired);
        validator.RuleForEach(x => x.Players).ChildRules(p =>
        {
            p.RuleFor(x => x.SpiritId).NotEmpty().WithDomainError(DomainErrors.Game.SpiritRequired);
            p.RuleFor(x => x.BoardId).NotEmpty().WithDomainError(DomainErrors.Game.BoardRequired);
            p.RuleFor(x => x).Must(x => x.UserId.HasValue || x.PlayerId.HasValue)
                .WithDomainError(DomainErrors.Game.AssigneeRequired)
                .OverridePropertyName("AssignedTo");
        });
        validator.RuleFor(x => x.ExtraBoardId).NotEmpty().WithDomainError(DomainErrors.Game.ExtraBoardRequired)
            .When(x => x.ExtraBoard);
        validator.RuleFor(x => x.ExtraBoardId).Empty().WithDomainError(DomainErrors.Game.ExtraBoardNotUsed)
            .When(x => !x.ExtraBoard);
        validator.RuleFor(x => x.DifficultyModifier)
            .InclusiveBetween(GameRestrictions.DifficultyModifierMin, GameRestrictions.DifficultyModifierMax)
            .WithDomainError(DomainErrors.Game.InvalidDifficultyModifier);
        validator.RuleFor(x => x.Note!).MaximumLength(GameRestrictions.NoteLength)
            .WithDomainError(DomainErrors.Game.NoteTooLong)
            .When(x => x.Note is not null);
    }
}
