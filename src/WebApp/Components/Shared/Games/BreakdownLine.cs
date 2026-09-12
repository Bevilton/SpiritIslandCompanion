namespace WebApp.Components.Shared.Games;

/// <summary>
/// One term of a figure that is explained as a sum — a difficulty's or a score's. What it is
/// called, and how much it adds: signed, since a modifier can take away, and rendered through
/// <see cref="GameFormat.Signed"/> wherever it is shown. Shared by <see cref="DifficultyBreakdown"/>
/// and <see cref="ScoreBreakdown"/>, whose lines are drawn by the same kind of list.
/// </summary>
public readonly record struct BreakdownLine(string Label, int Amount);
