using Domain.Results;

namespace Domain.Errors;

public static class DomainErrors
{
    public static class Game
    {
        public static Error NoteTooLong => Error.Validation("Game.NoteTooLong", "Note is too long");
        public static Error InvalidCardCount => Error.Validation("Game.InvalidCardCount", "Invalid card count");
        public static Error InvalidBlightCount => Error.Validation("Game.InvalidBlightCount", "Invalid blight count");
        public static Error InvalidDahanCount => Error.Validation("Game.InvalidDahanCount", "Invalid dahan count");
        public static Error InvalidScore => Error.Validation("Game.InvalidScore", "Invalid score");
        public static Error InvalidScoreModifier => Error.Validation("Game.InvalidScoreModifier", "Invalid score modifier");
        public static Error InvalidAdversaryLevel => Error.Validation("Game.InvalidAdversaryLevel", "Invalid adversary level");
        public static Error InvalidDifficulty => Error.Validation("Game.InvalidDifficulty", "Invalid difficulty");
        public static Error PlayerNotFriend => Error.Validation("Game.PlayerNotFriend", "You can only add registered users who are your friends.");
    }

    public static class Friendship
    {
        public static Error AlreadyExists => Error.Conflict("Friendship.AlreadyExists", "A friendship or pending request already exists between these users.");
        public static Error NotFound => Error.NotFound("Friendship.NotFound", "Friendship not found.");
        public static Error CannotFriendSelf => Error.Validation("Friendship.CannotFriendSelf", "You cannot send a friend request to yourself.");
        public static Error AlreadyResponded => Error.Validation("Friendship.AlreadyResponded", "This friend request has already been responded to.");
    }
}