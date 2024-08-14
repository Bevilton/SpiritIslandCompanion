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
    }
}