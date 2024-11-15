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

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    /// <summary>
    /// Empty constructor required for EF Core.
    /// </summary>
    [Obsolete("Empty constructor required for EF Core.")]
    private PlayedScenario(){}
#pragma warning restore
}