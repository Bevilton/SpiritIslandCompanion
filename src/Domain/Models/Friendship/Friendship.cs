using Domain.Errors;
using Domain.Models.User;
using Domain.Primitives;
using Domain.Results;

namespace Domain.Models.Friendship;

public class Friendship : AggregateRoot<FriendshipId>
{
    public UserId RequesterId { get; private init; }
    public UserId AddresseeId { get; private init; }
    public FriendshipStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private init; }
    public DateTimeOffset? RespondedAt { get; private set; }

    private Friendship(
        FriendshipId id,
        UserId requesterId,
        UserId addresseeId,
        FriendshipStatus status,
        DateTimeOffset createdAt)
        : base(id)
    {
        RequesterId = requesterId;
        AddresseeId = addresseeId;
        Status = status;
        CreatedAt = createdAt;
    }

    public static Result<Friendship> Create(FriendshipId id, UserId requesterId, UserId addresseeId, DateTimeOffset createdAt)
    {
        if (requesterId == addresseeId)
            return Result.Failure<Friendship>(DomainErrors.Friendship.CannotFriendSelf);

        return new Friendship(id, requesterId, addresseeId, FriendshipStatus.Pending, createdAt);
    }

    /// <summary>Accepts the request. Only the addressee can, and only while it is still open.</summary>
    public Result Accept(UserId responder, DateTimeOffset respondedAt)
    {
        var open = AwaitingAnswerFrom(responder);
        if (open.IsFailure)
            return open;

        Status = FriendshipStatus.Accepted;
        RespondedAt = respondedAt;
        return Result.Success();
    }

    /// <summary>
    /// Whether the request is still waiting on <paramref name="responder"/> to answer — the one
    /// condition under which it can be accepted, or declined. Declining keeps nothing: a request
    /// turned down is not a record of anything (nothing ever read one back, and the two may ask
    /// again later), so there is no declined state here — the caller removes the friendship once
    /// this says it may.
    /// </summary>
    public Result AwaitingAnswerFrom(UserId responder)
    {
        if (responder != AddresseeId)
            return Result.Failure(DomainErrors.Friendship.NotAddressee);
        if (Status != FriendshipStatus.Pending)
            return Result.Failure(DomainErrors.Friendship.AlreadyResponded);

        return Result.Success();
    }

    /// <summary>True if the given user is either the requester or the addressee.</summary>
    public bool InvolvesUser(UserId userId) =>
        RequesterId == userId || AddresseeId == userId;

    /// <summary>Given one side of the friendship, returns the other user's id.</summary>
    public UserId GetOtherUserId(UserId userId) =>
        RequesterId == userId ? AddresseeId : RequesterId;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    /// <summary>
    /// Empty constructor required for EF Core.
    /// </summary>
    [Obsolete("Empty constructor required for EF Core.")]
    private Friendship()
    {
    }
#pragma warning restore
}
