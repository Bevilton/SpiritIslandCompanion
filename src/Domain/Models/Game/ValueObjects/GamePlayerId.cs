using Domain.Primitives;

namespace Domain.Models.Game;

public record GamePlayerId(Guid Value) : Identifier(Value);