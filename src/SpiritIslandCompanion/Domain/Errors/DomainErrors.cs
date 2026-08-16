using Domain.Models.Game;
using Domain.Models.IslandLayout;
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
        public static Error NotFound => Error.NotFound("Player.NotFound", "Local player not found.");
        public static Error NotYours => Error.Forbidden("Player.NotYours", "That local player belongs to someone else.");
    }

    public static class User
    {
        public static Error NotFound => Error.NotFound("User.NotFound", "User not found.");
        public static Error EmailRequired => Error.Validation("User.EmailRequired", "Email is required.");
        public static Error EmailInvalid => Error.Validation("User.EmailInvalid", "Enter a valid email address.");
        public static Error NicknameRequired => Error.Validation("User.NicknameRequired", "Nickname is required.");
        public static Error NicknameTooLong => Error.Validation("User.NicknameTooLong", $"Nickname must be at most {Nickname.MaxLength} characters.");
        public static Error UnknownExpansion => Error.Validation("User.UnknownExpansion", "One of the selected expansions is not recognised.");
    }

    public static class Game
    {
        public static Error NotFound => Error.NotFound("Game.NotFound", "Game not found.");
        public static Error AlreadyCompleted => Error.Conflict("Game.AlreadyCompleted", "Game already has a result.");
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
        public static Error NoThematicMapForBoardCount => Error.Validation("Game.NoThematicMapForBoardCount", "There is no thematic island for that many boards — Spirit Island publishes thematic maps for 1–4 and 6 boards only.");
        public static Error IslandSetupIsThematic => Error.Validation("Game.IslandSetupIsThematic", "Thematic maps is off, but the selected layout is a thematic one.");
        public static Error ExtraBoardNotAllowed => Error.Validation("Game.ExtraBoardNotAllowed", "Extra board is only allowed for 1–5 players.");
        public static Error ExtraBoardRequired => Error.Validation("Game.ExtraBoardRequired", "Pick the extra board.");
        public static Error ExtraBoardNotUsed => Error.Validation("Game.ExtraBoardNotUsed", "A board was named as the extra one, but this game does not use an extra board.");
        public static Error ThematicBoardMismatch => Error.Validation("Game.ThematicBoardMismatch", "On the thematic island each seat plays the board belonging to its position — these boards don't match the island's regions.");
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

    public static class IslandLayout
    {
        public static Error NameRequired => Error.Validation("IslandLayout.NameRequired", "Give the layout a name so you can find it again.");
        public static Error NameTooLong => Error.Validation("IslandLayout.NameTooLong", $"Layout name must be at most {IslandLayoutName.MaxLength} characters.");
        public static Error GeometryRequired => Error.Validation("IslandLayout.GeometryRequired", "Arrange the boards on the island before saving the layout.");
        public static Error GeometryTooLong => Error.Validation("IslandLayout.GeometryTooLong", "The island arrangement is too large to store.");
        public static Error GeometryMalformed => Error.Validation("IslandLayout.GeometryMalformed", "The island arrangement could not be read — it does not describe a set of boards.");
        public static Error NotFound => Error.NotFound("IslandLayout.NotFound", "That saved layout no longer exists.");
        public static Error BoardCountMismatch => Error.Validation("IslandLayout.BoardCountMismatch", "The saved layout was built for a different number of boards.");
        public static Error InvalidBoardCount => Error.Validation("IslandLayout.InvalidBoardCount", $"A layout must cover between 1 and {GameRestrictions.MaximumBoards} boards.");
        public static Error NotAllowedWithThematicMaps => Error.Validation("IslandLayout.NotAllowedWithThematicMaps", "Thematic maps use their own fixed island — a hand-built layout can't be used with them.");
        public static Error InUse => Error.Conflict("IslandLayout.InUse", "This shape has recorded games — it can only be deleted once no game uses it.");
    }

    public static class Friendship
    {
        public static Error AlreadyExists => Error.Conflict("Friendship.AlreadyExists", "A friendship or pending request already exists between these users.");
        public static Error NotFound => Error.NotFound("Friendship.NotFound", "Friendship not found.");
        public static Error CannotFriendSelf => Error.Validation("Friendship.CannotFriendSelf", "You cannot send a friend request to yourself.");
        public static Error AlreadyResponded => Error.Validation("Friendship.AlreadyResponded", "This friend request has already been responded to.");
        public static Error NotAccepted => Error.Validation("Friendship.NotAccepted", "You are not friends with that user yet.");
    }

    public static class PlayerMerge
    {
        public static Error NotFound => Error.NotFound("PlayerMerge.NotFound", "That merge request no longer exists.");
        public static Error CannotMergeSelf => Error.Validation("PlayerMerge.CannotMergeSelf", "You cannot merge a local player into your own account.");
        public static Error AlreadyPending => Error.Conflict("PlayerMerge.AlreadyPending", "You already have a merge request out for this local player.");
        public static Error AlreadyResponded => Error.Validation("PlayerMerge.AlreadyResponded", "This merge request has already been responded to.");
        public static Error NotTarget => Error.Forbidden("PlayerMerge.NotTarget", "Only the person being merged into can answer this request.");
        public static Error NotRequester => Error.Forbidden("PlayerMerge.NotRequester", "Only the person who asked can withdraw this request.");
        public static Error NotInvolved => Error.Forbidden("PlayerMerge.NotInvolved", "This merge request is not yours to see.");

        public static Error SeatConflict => Error.Conflict(
            "PlayerMerge.SeatConflict",
            "A game seats both this local player and your account — merging would put you at the same table twice. Fix those games first.");
    }
}
