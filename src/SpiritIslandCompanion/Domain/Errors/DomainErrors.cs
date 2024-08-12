using Domain.Results;

namespace Domain.Errors;

public static class DomainErrors
{
    public static class Game
    {
        public static Error NoteTooLong => Error.Validation("Game.NoteTooLong", "Note is too long");
    }
}