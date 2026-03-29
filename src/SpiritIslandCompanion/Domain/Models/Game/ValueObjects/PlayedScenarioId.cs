using Domain.Primitives;

namespace Domain.Models.Game;

public record PlayedScenarioId(Guid Value) : Identifier(Value);