using Domain.Primitives;

namespace Domain.Models.Player;

public sealed record PlayerId(Guid Value) : Identifier(Value);