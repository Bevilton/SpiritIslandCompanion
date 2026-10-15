using Application.Abstractions;
using Application.Data;
using Application.Features.Friendships;
using Domain.Errors;
using Domain.Models.Player;
using Domain.Models.PlayerMerge;
using Domain.Models.User;
using Domain.Results;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.PlayerMerges;

/// <summary>
/// Asks a friend to take over a local player's seats — "this guest in my games is you".
/// Nothing moves until they approve; see <see cref="PlayerMergeRequest"/>.
/// </summary>
public sealed record SendMergeRequestCommand(Guid RequesterId, Guid PlayerId, Guid TargetUserId) : ICommand;

internal sealed class SendMergeRequestHandler(IAppDbContext db, MergeSubjectGames subjectGames, TimeProvider clock)
    : ICommandHandler<SendMergeRequestCommand>
{
    public async Task<Result> Handle(SendMergeRequestCommand request, CancellationToken cancellationToken)
    {
        var requesterId = new UserId(request.RequesterId);
        var targetId = new UserId(request.TargetUserId);
        var playerId = new PlayerId(request.PlayerId);

        var player = await db.Players
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == playerId, cancellationToken);

        if (player is null)
            return Result.Failure(DomainErrors.Player.NotFound);
        if (player.CreatedBy != requesterId)
            return Result.Failure(DomainErrors.Player.NotYours);

        // Written before anything is looked up about the two accounts: what the request itself
        // refuses — asking yourself — is answered as such, not as a friendship that is missing.
        var merge = PlayerMergeRequest.Create(
            new PlayerMergeRequestId(Guid.NewGuid()), playerId, requesterId, targetId, clock.GetUtcNow());

        if (merge.IsFailure)
            return Result.Failure(merge.Error);

        // Only a friend can be asked: the merge hands them the seats and, with them, a place
        // in each other's game history — the same bar CreateGame sets for seating an account.
        var areFriends = await db.Friendships
            .AsNoTracking()
            .Accepted()
            .Between(requesterId, targetId)
            .AnyAsync(cancellationToken);

        if (!areFriends)
            return Result.Failure(DomainErrors.Friendship.NotAccepted);

        var pending = await db.PlayerMergeRequests
            .AsNoTracking()
            .About(playerId)
            .Pending()
            .AnyAsync(cancellationToken);

        if (pending)
            return Result.Failure(DomainErrors.PlayerMerge.AlreadyPending);

        // Caught here as well as on approval (see Game.HandSeatsToUser), so the person asking
        // is the one who finds out.
        var games = await subjectGames.LoadAsync(playerId, tracked: false, cancellationToken);
        if (games.Any(g => g.SeatsBoth(playerId, targetId)))
            return Result.Failure(DomainErrors.PlayerMerge.SeatConflict);

        db.PlayerMergeRequests.Add(merge.Value);
        return Result.Success();
    }
}
