namespace Domain.Models.Static;

/// <summary>One level an adversary can be fought at, and the difficulty that level adds.</summary>
public sealed record AdversaryMode(int Level, int Difficulty);
