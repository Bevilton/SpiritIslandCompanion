using Domain.Models.Game.Enums;
using Domain.Primitives;

namespace Domain.Models.Game;

public class GameResult : Entity<GameResultId>
{
    public bool Win { get; private set; }
    public TimeSpan Duration { get; private set; }
    public CardsCount Cards { get; private set; }
    public TerrorLevel TerrorLevel { get; private set; }
    public BlightCount Blight { get; private set; }
    public DahanCount Dahan { get; private set; }
    public Score Score { get; private set; }
    public ScoreModifier ScoreModifier { get; private set; }


    private GameResult(GameResultId id, bool win, TimeSpan duration, CardsCount cards, TerrorLevel terrorLevel, BlightCount blight, DahanCount dahan, Score score, ScoreModifier scoreModifier) : base(id)
    {
        Win = win;
        Duration = duration;
        Cards = cards;
        TerrorLevel = terrorLevel;
        Blight = blight;
        Dahan = dahan;
        Score = score;
        ScoreModifier = scoreModifier;
    }

    public static GameResult Create(GameResultId id, bool win, TimeSpan duration, CardsCount cards, TerrorLevel terrorLevel, BlightCount blight, DahanCount dahan, Score score, ScoreModifier scoreModifier)
    {
        return new GameResult(id, win, duration, cards, terrorLevel, blight, dahan, score, scoreModifier);
    }

    public void Update(bool win, TimeSpan duration, CardsCount cards, TerrorLevel terrorLevel, BlightCount blight, DahanCount dahan, Score score, ScoreModifier scoreModifier)
    {
        Win = win;
        Duration = duration;
        Cards = cards;
        TerrorLevel = terrorLevel;
        Blight = blight;
        Dahan = dahan;
        Score = score;
        ScoreModifier = scoreModifier;
    }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    /// <summary>
    /// Empty constructor required for EF Core.
    /// </summary>
    [Obsolete("Empty constructor required for EF Core.")]
    private GameResult(){}
#pragma warning restore
}