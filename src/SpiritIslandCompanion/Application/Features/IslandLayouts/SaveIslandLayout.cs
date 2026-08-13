using Application.Abstractions;
using Application.Behaviour;
using Application.Data;
using Domain.Errors;
using Domain.Models.IslandLayout;
using Domain.Models.User;
using Domain.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.IslandLayouts;

/// <summary>
/// Adds a hand-built arrangement to the caller's layout library, or updates one already
/// there. The id comes from the caller so the form can reference the layout it just saved
/// without a second round-trip; an unknown id is treated as a new layout, and one that
/// exists under another owner is refused, so a Save can never touch a stranger's row.
/// <para>
/// How many boards the shape covers is not asked for — it is counted off the arrangement
/// itself, so the two can't disagree.
/// </para>
/// </summary>
public sealed record SaveIslandLayoutCommand(
    Guid LayoutId,
    Guid OwnerId,
    string Name,
    string LayoutJson,
    DateTimeOffset SavedAt) : ICommand;

internal sealed class SaveIslandLayoutValidator : AbstractValidator<SaveIslandLayoutCommand>
{
    public SaveIslandLayoutValidator()
    {
        // Measured the way IslandLayoutName.Create measures — trimmed — so the validator
        // can never refuse a name the domain would accept, or the other way round.
        RuleFor(x => x.Name)
            .Must(n => !string.IsNullOrWhiteSpace(n)).WithDomainError(DomainErrors.IslandLayout.NameRequired)
            .Must(n => n is null || n.Trim().Length <= IslandLayoutName.MaxLength)
                .WithDomainError(DomainErrors.IslandLayout.NameTooLong);
        // Everything past "there is something, and it isn't unbounded" is the geometry's
        // own business — see IslandLayoutGeometry.Create.
        RuleFor(x => x.LayoutJson)
            .NotEmpty().WithDomainError(DomainErrors.IslandLayout.GeometryRequired)
            .MaximumLength(IslandLayoutGeometry.MaxLength).WithDomainError(DomainErrors.IslandLayout.GeometryTooLong);
    }
}

internal sealed class SaveIslandLayoutHandler(IAppDbContext db) : ICommandHandler<SaveIslandLayoutCommand>
{
    public async Task<Result> Handle(SaveIslandLayoutCommand request, CancellationToken cancellationToken)
    {
        var nameResult = IslandLayoutName.Create(request.Name);
        if (nameResult.IsFailure)
            return Result.Failure(nameResult.Error);

        var geometryResult = IslandLayoutGeometry.Create(request.LayoutJson);
        if (geometryResult.IsFailure)
            return Result.Failure(geometryResult.Error);

        // Compare the id itself, not its .Value: the key goes through a value converter, and
        // only the whole key translates to SQL.
        var layoutId = new CustomIslandLayoutId(request.LayoutId);
        var existing = await db.CustomIslandLayouts.FirstOrDefaultAsync(
            l => l.Id == layoutId && l.OwnerId.Value == request.OwnerId,
            cancellationToken);

        if (existing is not null)
            return existing.Update(nameResult.Value, geometryResult.Value, request.SavedAt);

        // The id is the caller's to mint, not to reuse: one that already exists under another
        // owner has to fail here, or the insert below becomes a primary-key collision.
        if (await db.CustomIslandLayouts.AnyAsync(l => l.Id == layoutId, cancellationToken))
            return Result.Failure(DomainErrors.IslandLayout.NotFound);

        db.CustomIslandLayouts.Add(CustomIslandLayout.Create(
            layoutId,
            new UserId(request.OwnerId),
            nameResult.Value,
            geometryResult.Value,
            request.SavedAt));

        return Result.Success();
    }
}
