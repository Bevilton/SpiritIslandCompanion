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
        public static Error NameRequired { get; } = new("Player.NameRequired", "Player name is required.");
        public static Error NameTooLong { get; } = new("Player.NameTooLong", $"Player name must be at most {PlayerName.MaxLength} characters.");
        public static Error NotFound { get; } = new("Player.NotFound", "Local player not found.");
        public static Error NotYours { get; } = new("Player.NotYours", "That local player belongs to someone else.");
    }

    public static class User
    {
        public static Error NotFound { get; } = new("User.NotFound", "User not found.");
        public static Error NotFoundByEmail { get; } = new("User.NotFoundByEmail", "No user found with that email.");
        public static Error EmailRequired { get; } = new("User.EmailRequired", "Email is required.");
        public static Error EmailInvalid { get; } = new("User.EmailInvalid", "Enter a valid email address.");
        public static Error EmailTooLong { get; } = new("User.EmailTooLong", $"Email must be at most {Email.MaxLength} characters.");
        public static Error NicknameRequired { get; } = new("User.NicknameRequired", "Nickname is required.");
        public static Error NicknameTooLong { get; } = new("User.NicknameTooLong", $"Nickname must be at most {Nickname.MaxLength} characters.");
        public static Error UnknownExpansion { get; } = new("User.UnknownExpansion", "One of the selected expansions is not recognised.");
    }

    public static class Game
    {
        public static Error NotFound { get; } = new("Game.NotFound", "Game not found.");
        public static Error AlreadyCompleted { get; } = new("Game.AlreadyCompleted", "Game already has a result.");
        public static Error NoteTooLong { get; } = new("Game.NoteTooLong", $"Note must be at most {GameRestrictions.MaximumNoteLength} characters.");
        public static Error InvalidCardCount { get; } = new("Game.InvalidCardCount", $"Cards must be between 0 and {GameRestrictions.MaximumCardsCount}.");
        public static Error InvalidBlightCount { get; } = new("Game.InvalidBlightCount", $"Blight must be between 0 and {GameRestrictions.MaximumBlightCount}.");
        public static Error InvalidDahanCount { get; } = new("Game.InvalidDahanCount", $"Dahan must be between 0 and {GameRestrictions.MaximumDahanCount}.");
        public static Error InvalidScoreModifier { get; } = new("Game.InvalidScoreModifier", $"Score modifier must be between {GameRestrictions.MinimumScoreModifier} and {GameRestrictions.MaximumScoreModifier}.");
        public static Error InvalidAdversaryLevel { get; } = new("Game.InvalidAdversaryLevel", $"Adversary level must be between 0 and {GameRestrictions.MaximumAdversaryLevel}.");
        public static Error InvalidDifficultyModifier { get; } = new("Game.InvalidDifficultyModifier", $"Difficulty modifier must be between {GameRestrictions.MinimumDifficultyModifier} and {GameRestrictions.MaximumDifficultyModifier}.");
        public static Error UnknownIslandSetup { get; } = new("Game.UnknownIslandSetup", "The selected island setup does not exist.");
        public static Error IslandSetupPlayerCountMismatch { get; } = new("Game.IslandSetupPlayerCountMismatch", "The selected island setup does not match the number of players (and extra board, if any).");
        public static Error ExtraBoardNotAllowed { get; } = new("Game.ExtraBoardNotAllowed", "Extra board is only allowed for 1–5 players.");
        public static Error ExtraBoardRequired { get; } = new("Game.ExtraBoardRequired", "Pick the extra board.");
        public static Error ExtraBoardNotUsed { get; } = new("Game.ExtraBoardNotUsed", "A board was named as the extra one, but this game does not use an extra board.");
        public static Error ThematicBoardMismatch { get; } = new("Game.ThematicBoardMismatch", "On the thematic island each seat plays the board belonging to its position — these boards don't match the island's regions.");
        public static Error PlayerNotFriend { get; } = new("Game.PlayerNotFriend", "You can only add registered users who are your friends.");

        public static Error IslandSetupRequired { get; } = new("Game.IslandSetupRequired", "Pick an island setup.");
        public static Error PlayersRequired { get; } = new("Game.PlayersRequired", "Add at least one player.");
        public static Error ResultRequired { get; } = new("Game.ResultRequired", "Game result is required.");
        public static Error SpiritRequired { get; } = new("Game.SpiritRequired", "Pick a spirit for each player.");
        public static Error BoardRequired { get; } = new("Game.BoardRequired", "Pick a board for each player.");
        public static Error AssigneeRequired { get; } = new("Game.AssigneeRequired", "Each player must be assigned to someone.");
        public static Error DurationNegative { get; } = new("Game.DurationNegative", "Duration cannot be negative.");
        public static Error DurationTooLong { get; } = new("Game.DurationTooLong", $"Duration must be under {GameRestrictions.MaximumDuration.TotalHours:0} hours.");
        public static Error InvalidTerrorLevel { get; } = new("Game.InvalidTerrorLevel", "Invalid terror level.");
        public static Error UnknownSpirit { get; } = new("Game.UnknownSpirit", "Selected spirit is not recognised.");
        public static Error UnknownBoard { get; } = new("Game.UnknownBoard", "Selected board is not recognised.");
        public static Error UnknownAspect { get; } = new("Game.UnknownAspect", "Selected aspect is not recognised.");
        public static Error AspectMismatch { get; } = new("Game.AspectMismatch", "The selected aspect belongs to a different spirit.");
        public static Error UnknownAdversary { get; } = new("Game.UnknownAdversary", "Selected adversary is not recognised.");
        public static Error UnknownAdversaryLevel { get; } = new("Game.UnknownAdversaryLevel", "Selected adversary level is not available for that adversary.");
        public static Error UnknownScenario { get; } = new("Game.UnknownScenario", "Selected scenario is not recognised.");
        public static Error DuplicateBoard { get; } = new("Game.DuplicateBoard", "Each player must play on a different board.");
        public static Error DuplicateAdversary { get; } = new("Game.DuplicateAdversary", "The same adversary cannot be added more than once.");
    }

    public static class IslandLayout
    {
        public static Error NameRequired { get; } = new("IslandLayout.NameRequired", "Give the layout a name so you can find it again.");
        public static Error NameTooLong { get; } = new("IslandLayout.NameTooLong", $"Layout name must be at most {IslandLayoutName.MaxLength} characters.");
        public static Error GeometryRequired { get; } = new("IslandLayout.GeometryRequired", "Arrange the boards on the island before saving the layout.");
        public static Error GeometryTooLong { get; } = new("IslandLayout.GeometryTooLong", "The island arrangement is too large to store.");
        public static Error GeometryMalformed { get; } = new("IslandLayout.GeometryMalformed", "The island arrangement could not be read — it does not describe a set of boards.");
        public static Error NotFound { get; } = new("IslandLayout.NotFound", "That saved layout no longer exists.");
        public static Error BoardCountMismatch { get; } = new("IslandLayout.BoardCountMismatch", "The saved layout was built for a different number of boards.");
        public static Error InvalidBoardCount { get; } = new("IslandLayout.InvalidBoardCount", $"A layout must cover between 1 and {GameRestrictions.MaximumBoards} boards.");
        public static Error InUse { get; } = new("IslandLayout.InUse", "This shape has recorded games — it can only be deleted once no game uses it.");
    }

    public static class Friendship
    {
        public static Error AlreadyExists { get; } = new("Friendship.AlreadyExists", "A friendship or pending request already exists between these users.");
        public static Error NotFound { get; } = new("Friendship.NotFound", "Friendship not found.");
        public static Error CannotFriendSelf { get; } = new("Friendship.CannotFriendSelf", "You cannot send a friend request to yourself.");
        public static Error AlreadyResponded { get; } = new("Friendship.AlreadyResponded", "This friend request has already been responded to.");
        public static Error NotAccepted { get; } = new("Friendship.NotAccepted", "You are not friends with that user yet.");
        public static Error NotAddressee { get; } = new("Friendship.NotAddressee", "Only the recipient can answer a friend request.");
        public static Error NotInvolved { get; } = new("Friendship.NotInvolved", "You are not part of this friendship.");
    }

    public static class PlayerMerge
    {
        public static Error NotFound { get; } = new("PlayerMerge.NotFound", "That merge request no longer exists.");
        public static Error CannotMergeSelf { get; } = new("PlayerMerge.CannotMergeSelf", "You cannot merge a local player into your own account.");
        public static Error AlreadyPending { get; } = new("PlayerMerge.AlreadyPending", "You already have a merge request out for this local player.");
        public static Error AlreadyResponded { get; } = new("PlayerMerge.AlreadyResponded", "This merge request has already been responded to.");
        public static Error NotTarget { get; } = new("PlayerMerge.NotTarget", "Only the person being merged into can answer this request.");
        public static Error NotRequester { get; } = new("PlayerMerge.NotRequester", "Only the person who asked can withdraw this request.");
        public static Error NotInvolved { get; } = new("PlayerMerge.NotInvolved", "This merge request is not yours to see.");

        public static Error SeatConflict { get; } = new(
            "PlayerMerge.SeatConflict",
            "A game seats both this local player and your account — merging would put you at the same table twice. Fix those games first.");
    }
}
