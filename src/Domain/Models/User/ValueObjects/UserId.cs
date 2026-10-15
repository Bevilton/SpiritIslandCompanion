using Domain.Primitives;

namespace Domain.Models.User;

public sealed record UserId(Guid Value) : Identifier(Value);