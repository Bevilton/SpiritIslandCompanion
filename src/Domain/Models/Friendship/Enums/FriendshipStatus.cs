namespace Domain.Models.Friendship;

/// <summary>
/// Where a friendship stands. There is no declined state: a request turned down is removed
/// rather than kept, since nothing ever reads one back (see <see cref="Friendship.AwaitingAnswerFrom"/>).
/// </summary>
public enum FriendshipStatus
{
    Pending,
    Accepted
}
