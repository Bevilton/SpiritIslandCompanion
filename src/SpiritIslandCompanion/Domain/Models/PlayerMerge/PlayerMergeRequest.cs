using Domain.Errors;
using Domain.Models.Player;
using Domain.Models.User;
using Domain.Primitives;
using Domain.Results;

namespace Domain.Models.PlayerMerge;

/// <summary>
/// One user asking another "this local player in my games is you — take the seats".
/// <para>
/// The asking is one-directional and the answering is the other way round on purpose. Only
/// the owner of a local player knows which of their guests it stands for, and only the
/// account being pointed at can say whether that is really them — so the owner asks and the
/// target answers. Everyone who recorded games with the same person keeps their own local
/// player and sends their own request; approving one says nothing about the others.
/// </para>
/// </summary>
public class PlayerMergeRequest : AggregateRoot<PlayerMergeRequestId>
{
    /// <summary>The local player whose seats would be handed over.</summary>
    public PlayerId PlayerId { get; private init; }

    /// <summary>The owner of that local player — the one asking.</summary>
    public UserId RequesterId { get; private init; }

    /// <summary>The account the seats would move to — the one who has to agree.</summary>
    public UserId TargetUserId { get; private init; }

    public PlayerMergeStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private init; }
    public DateTimeOffset? RespondedAt { get; private set; }

    private PlayerMergeRequest(
        PlayerMergeRequestId id,
        PlayerId playerId,
        UserId requesterId,
        UserId targetUserId,
        PlayerMergeStatus status,
        DateTimeOffset createdAt)
        : base(id)
    {
        PlayerId = playerId;
        RequesterId = requesterId;
        TargetUserId = targetUserId;
        Status = status;
        CreatedAt = createdAt;
    }

    public static Result<PlayerMergeRequest> Create(
        PlayerMergeRequestId id,
        PlayerId playerId,
        UserId requesterId,
        UserId targetUserId)
    {
        if (requesterId == targetUserId)
            return Result.Failure<PlayerMergeRequest>(DomainErrors.PlayerMerge.CannotMergeSelf);

        return new PlayerMergeRequest(
            id, playerId, requesterId, targetUserId, PlayerMergeStatus.Pending, DateTimeOffset.UtcNow);
    }

    public Result Approve() => Respond(PlayerMergeStatus.Approved);

    public Result Reject() => Respond(PlayerMergeStatus.Rejected);

    private Result Respond(PlayerMergeStatus status)
    {
        if (Status != PlayerMergeStatus.Pending)
            return Result.Failure(DomainErrors.PlayerMerge.AlreadyResponded);

        Status = status;
        RespondedAt = DateTimeOffset.UtcNow;
        return Result.Success();
    }

    /// <summary>True if the given user is either side of this request.</summary>
    public bool InvolvesUser(UserId userId) =>
        RequesterId == userId || TargetUserId == userId;

#pragma warning disable CS8618
    /// <summary>
    /// Empty constructor required for EF Core.
    /// </summary>
    [Obsolete("Empty constructor required for EF Core.")]
    private PlayerMergeRequest(){}
#pragma warning restore
}
