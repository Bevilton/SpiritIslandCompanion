namespace Domain.Models.Static;

/// <param name="Modes">The levels the adversary can be fought at, each with the difficulty it adds.</param>
public sealed record Adversary(AdversaryId Id, string Name, ExpansionId ExpansionId, IReadOnlyList<AdversaryMode> Modes);
