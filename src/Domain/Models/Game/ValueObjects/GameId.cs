using Domain.Primitives;

namespace Domain.Models.Game;

public record GameId(Guid Value) : Identifier(Value);