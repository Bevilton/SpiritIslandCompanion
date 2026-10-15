using Domain.Primitives;

namespace Domain.Models.Game;

public sealed record PlayedAdversaryId(Guid Value) : Identifier(Value);