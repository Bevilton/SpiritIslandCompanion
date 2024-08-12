using Domain.Primitives;

namespace Domain.Models.Game;

public class GameResult : Entity<GameResultId>
{
    public bool Win { get; private set; }
    public TimeSpan Duration { get; private set; }
    public uint Cards { get; private set; }
    public uint TerrorLevel { get; private set; }
    public uint Blight { get; private set; }
    public uint Dahan { get; private set; }
    public uint Score { get; private set; }
    public uint ScoreModifier { get; private set; }
}