using Domain.Primitives;

namespace Domain.Models.Game;

internal record PlayedScenarioId(Guid Value) : Identifier(Value);