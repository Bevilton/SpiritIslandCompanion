using Domain.Primitives;

namespace Domain.Models.User;

public record UserId(Guid Value) : Identifier(Value);