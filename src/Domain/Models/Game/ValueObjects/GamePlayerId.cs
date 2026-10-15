using Domain.Primitives;

namespace Domain.Models.Game;

public sealed record GamePlayerId(Guid Value) : Identifier(Value);