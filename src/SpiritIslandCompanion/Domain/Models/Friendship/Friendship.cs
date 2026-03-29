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

    public static Result<Friendship> Create(FriendshipId id, UserId requesterId, UserId addresseeId)
    {
        if (requesterId == addresseeId)
            return Result.Failure<Friendship>(DomainErrors.Friendship.CannotFriendSelf);

        var friendship = new Friendship(id, requesterId, addresseeId, FriendshipStatus.Pending, DateTimeOffset.UtcNow);
        return friendship;
    }

    public Result Accept()
    {
        if (Status != FriendshipStatus.Pending)
            return Result.Failure(DomainErrors.Friendship.AlreadyResponded);

        Status = FriendshipStatus.Accepted;
        RespondedAt = DateTimeOffset.UtcNow;
        return Result.Success();
    }

    public Result Reject()
    {
        if (Status != FriendshipStatus.Pending)
            return Result.Failure(DomainErrors.Friendship.AlreadyResponded);

        Status = FriendshipStatus.Rejected;
        RespondedAt = DateTimeOffset.UtcNow;
        return Result.Success();
    }

    /// <summary>
    /// Returns true if the given user is either the requester or the addressee.
    /// </summary>
    public bool InvolvesUser(UserId userId) =>
        RequesterId == userId || AddresseeId == userId;

    /// <summary>
    /// Given one side of the friendship, returns the other user's ID.
    /// </summary>
    public UserId GetOtherUserId(UserId userId) =>
        RequesterId == userId ? AddresseeId : RequesterId;

#pragma warning disable CS8618
    /// <summary>
    /// Empty constructor required for EF Core.
    /// </summary>
    [Obsolete("Empty constructor required for EF Core.")]
    private Friendship(){}
#pragma warning restore
}
