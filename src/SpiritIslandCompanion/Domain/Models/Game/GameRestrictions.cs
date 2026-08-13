namespace Domain.Models.Game;

public static class GameRestrictions
{
    public const int NoteLength = 1000;
    public const int MaximumCardsCount = 15;
    public const int MaximumBlightCount = 1000;
    public const int MaximumDahanCount = 1000;
    public const int MaximumScore = 1000;
    public const int MaximumScoreModifier = 1000;
    public const int MinimumScoreModifier = -1000;
    public const int MaximumAdversaryLevel = 6;
    public const int MaximumDifficulty = 20;

    public const int DifficultyModifierMin = -20;
    public const int DifficultyModifierMax = 20;
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
