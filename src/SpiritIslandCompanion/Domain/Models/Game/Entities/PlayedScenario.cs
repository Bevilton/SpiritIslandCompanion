using Domain.Models.Static;
using Domain.Primitives;

namespace Domain.Models.Game;

public class PlayedScenario : Entity<PlayedAdversaryId>
{
    public ScenarioId ScenarioId { get; private set; }

    public PlayedScenario(PlayedAdversaryId id, ScenarioId scenarioId) : base(id)
    {
        ScenarioId = scenarioId;
    }
}