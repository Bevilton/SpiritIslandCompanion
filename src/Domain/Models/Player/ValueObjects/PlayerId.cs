using Domain.Primitives;

namespace Domain.Models.Player;

public record PlayerId(Guid Value) : Identifier(Value);