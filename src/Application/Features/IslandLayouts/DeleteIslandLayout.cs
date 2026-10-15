using Application.Abstractions;
using Application.Data;
using Domain.Errors;
using Domain.Models.IslandLayout;
using Domain.Models.User;
using Domain.Results;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.IslandLayouts;

/// <summary>
/// Drops a layout from the caller's library — but only while no recorded game points at it.
/// Games keep their own copy of the arrangement, so history would survive the delete; what
/// wouldn't is their <c>CustomLayoutId</c> reference and the per-layout record built on it.
/// </summary>
public sealed record DeleteIslandLayoutCommand(Guid LayoutId, Guid UserId) : ICommand;

internal sealed class DeleteIslandLayoutHandler(IAppDbContext db) : ICommandHandler<DeleteIslandLayoutCommand>
{
    public async Task<Result> Handle(DeleteIslandLayoutCommand request, CancellationToken cancellationToken)
    {
        var layoutId = new CustomIslandLayoutId(request.LayoutId);
        var ownerId = new UserId(request.UserId);
        var layout = await db.CustomIslandLayouts.FirstOrDefaultAsync(
            l => l.Id == layoutId && l.OwnerId == ownerId,
            cancellationToken);

        if (layout is null)
            return Result.Failure(DomainErrors.IslandLayout.NotFound);

        if (await db.Games.AnyAsync(g => g.CustomLayoutId == layoutId, cancellationToken))
            return Result.Failure(DomainErrors.IslandLayout.InUse);

        db.CustomIslandLayouts.Remove(layout);
        return Result.Success();
    }
}
