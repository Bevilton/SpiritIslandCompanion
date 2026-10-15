using Domain.Primitives;

namespace Domain.Models.Friendship;

public sealed record FriendshipId(Guid Value) : Identifier(Value);
