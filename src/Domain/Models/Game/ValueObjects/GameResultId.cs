using Domain.Primitives;

namespace Domain.Models.Game;

public record GameResultId(Guid Value) : Identifier(Value);