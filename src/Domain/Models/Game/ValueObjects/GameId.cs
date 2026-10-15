using Domain.Primitives;

namespace Domain.Models.Game;

public sealed record GameId(Guid Value) : Identifier(Value);