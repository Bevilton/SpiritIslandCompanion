using Domain.Primitives;

namespace Domain.Models.Friendship;

public record FriendshipId(Guid Value) : Identifier(Value);
