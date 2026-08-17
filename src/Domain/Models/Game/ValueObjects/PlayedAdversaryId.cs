using Domain.Primitives;

namespace Domain.Models.Game;

public record PlayedAdversaryId(Guid Value) : Identifier(Value);