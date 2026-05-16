using Domain.Models.Game;
using Domain.Models.Player;
using Domain.Models.User;
using Domain.Results;

namespace Domain.Errors;

public static class DomainErrors
{
    public static class Player
    {
        public static Error NameRequired => Error.Validation("Player.NameRequired", "Player name is required.");
        public static Error NameTooLong => Error.Validation("Player.NameTooLong", $"Player name must be at most {PlayerName.MaxLength} characters.");
    }

    public static class User
    {
        public static Error EmailRequired => Error.Validation("User.EmailRequired", "Email is required.");
        public static Error EmailInvalid => Error.Validation("User.EmailInvalid", "Enter a valid email address.");
        public static Error NicknameRequired => Error.Validation("User.NicknameRequired", "Nickname is required.");
        public static Error NicknameTooLong => Error.Validation("User.NicknameTooLong", $"Nickname must be at most {Nickname.MaxLength} characters.");
        public static Error UnknownExpansion => Error.Validation("User.UnknownExpansion", "One of the selected expansions is not recognised.");
    }

    public static class Game
    {
        public static Error NoteTooLong => Error.Validation("Game.NoteTooLong", $"Note must be at most {GameRestrictions.NoteLength} characters.");
        public static Error InvalidCardCount => Error.Validation("Game.InvalidCardCount", $"Cards must be between 0 and {GameRestrictions.MaximumCardsCount}.");
        public static Error InvalidBlightCount => Error.Validation("Game.InvalidBlightCount", $"Blight must be between 0 and {GameRestrictions.MaximumBlightCount}.");
        public static Error InvalidDahanCount => Error.Validation("Game.InvalidDahanCount", $"Dahan must be between 0 and {GameRestrictions.MaximumDahanCount}.");
        public static Error InvalidScore => Error.Validation("Game.InvalidScore", $"Score must be between 0 and {GameRestrictions.MaximumScore}.");
        public static Error InvalidScoreModifier => Error.Validation("Game.InvalidScoreModifier", $"Score modifier must be between {GameRestrictions.MinimumScoreModifier} and {GameRestrictions.MaximumScoreModifier}.");
        public static Error InvalidAdversaryLevel => Error.Validation("Game.InvalidAdversaryLevel", $"Adversary level must be between 0 and {GameRestrictions.MaximumAdversaryLevel}.");
        public static Error InvalidDifficulty => Error.Validation("Game.InvalidDifficulty", $"Difficulty must be between 0 and {GameRestrictions.MaximumDifficulty}.");
        public static Error InvalidDifficultyModifier => Error.Validation("Game.InvalidDifficultyModifier", $"Difficulty modifier must be between {GameRestrictions.DifficultyModifierMin} and {GameRestrictions.DifficultyModifierMax}.");
        public static Error UnknownIslandSetup => Error.Validation("Game.UnknownIslandSetup", "The selected island setup does not exist.");
        public static Error IslandSetupPlayerCountMismatch => Error.Validation("Game.IslandSetupPlayerCountMismatch", "The selected island setup does not match the number of players (and extra board, if any).");
        public static Error IslandSetupNotThematic => Error.Validation("Game.IslandSetupNotThematic", "Thematic maps is on, but the selected layout is not a thematic one.");
        public static Error IslandSetupIsThematic => Error.Validation("Game.IslandSetupIsThematic", "Thematic maps is off, but the selected layout is a thematic one.");
        public static Error ExtraBoardNotAllowed => Error.Validation("Game.ExtraBoardNotAllowed", "Extra board is only allowed for 1–5 players.");
        public static Error PlayerNotFriend => Error.Validation("Game.PlayerNotFriend", "You can only add registered users who are your friends.");

        public static Error IslandSetupRequired => Error.Validation("Game.IslandSetupRequired", "Pick an island setup.");
        public static Error PlayersRequired => Error.Validation("Game.PlayersRequired", "Add at least one player.");
        public static Error ResultRequired => Error.Validation("Game.ResultRequired", "Game result is required.");
        public static Error SpiritRequired => Error.Validation("Game.SpiritRequired", "Pick a spirit for each player.");
        public static Error BoardRequired => Error.Validation("Game.BoardRequired", "Pick a board for each player.");
        public static Error AssigneeRequired => Error.Validation("Game.AssigneeRequired", "Each player must be assigned to someone.");
        public static Error DurationNegative => Error.Validation("Game.DurationNegative", "Duration cannot be negative.");
        public static Error InvalidTerrorLevel => Error.Validation("Game.InvalidTerrorLevel", "Invalid terror level.");
        public static Error UnknownSpirit => Error.Validation("Game.UnknownSpirit", "Selected spirit is not recognised.");
        public static Error UnknownBoard => Error.Validation("Game.UnknownBoard", "Selected board is not recognised.");
        public static Error UnknownAspect => Error.Validation("Game.UnknownAspect", "Selected aspect is not recognised.");
        public static Error UnknownAdversary => Error.Validation("Game.UnknownAdversary", "Selected adversary is not recognised.");
        public static Error UnknownAdversaryLevel => Error.Validation("Game.UnknownAdversaryLevel", "Selected adversary level is not available for that adversary.");
        public static Error UnknownScenario => Error.Validation("Game.UnknownScenario", "Selected scenario is not recognised.");
        public static Error DuplicateBoard => Error.Validation("Game.DuplicateBoard", "Each player must play on a different board.");
        public static Error DuplicateAdversary => Error.Validation("Game.DuplicateAdversary", "The same adversary cannot be added more than once.");
    }

    public static class Friendship
    {
        public static Error AlreadyExists => Error.Conflict("Friendship.AlreadyExists", "A friendship or pending request already exists between these users.");
        public static Error NotFound => Error.NotFound("Friendship.NotFound", "Friendship not found.");
        public static Error CannotFriendSelf => Error.Validation("Friendship.CannotFriendSelf", "You cannot send a friend request to yourself.");
        public static Error AlreadyResponded => Error.Validation("Friendship.AlreadyResponded", "This friend request has already been responded to.");
    }
}
