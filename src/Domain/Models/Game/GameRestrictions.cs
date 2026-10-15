namespace Domain.Models.Game;

public static class GameRestrictions
{
    public const int MaximumNoteLength = 1000;
    public const int MaximumCardsCount = 15;
    public const int MaximumBlightCount = 1000;
    public const int MaximumDahanCount = 1000;
    public const int MaximumScore = 1000;
    public const int MaximumScoreModifier = 1000;
    public const int MinimumScoreModifier = -1000;
    public const int MaximumAdversaryLevel = 6;
    public const int MaximumDifficulty = 20;

    /// <summary>
    /// A game lasts less than a day — this bound is exclusive. Also what the storage can hold:
    /// the duration is kept in a column of SQL Server's <c>time</c> type, which stops just short
    /// of 24 hours, so a longer game has to be refused here rather than fail on the way in.
    /// </summary>
    public static readonly TimeSpan MaximumDuration = TimeSpan.FromHours(24);

    public const int MinimumDifficultyModifier = -20;
    public const int MaximumDifficultyModifier = 20;
    public const int ExtraBoardDifficultyBonus = 2;
    public const int ThematicMapsDifficultyBonus = 3;
    public const int MaximumPlayersForExtraBoard = 5;

    public const int MaximumPlayers = 6;

    /// <summary>
    /// Boards on the island: one per player, plus the optional extra board. The extra board
    /// stops at <see cref="MaximumPlayersForExtraBoard"/> players, so the island never grows
    /// past a full table either way.
    /// </summary>
    public const int MaximumBoards = MaximumPlayers;
}
