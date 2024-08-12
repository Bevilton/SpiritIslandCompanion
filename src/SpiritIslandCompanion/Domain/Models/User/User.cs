using Domain.Primitives;

namespace Domain.Models.User;

public class User : AggregateRoot<UserId>
{
    public Email Email { get; private init; }
    public Nickname Nickname { get; private init; }
    public UserSettings UserSettings { get; private set; }
    public DateTimeOffset Registered { get; private set; }

    private User(UserId id, Email email, Nickname nickname, UserSettings userSettings, DateTimeOffset registered) : base(id)
    {
        Email = email;
        Nickname = nickname;
        UserSettings = userSettings;
        Registered = registered;
    }

    public static User Create(UserId id, Email email, Nickname nickname, UserSettings userSettings, DateTimeOffset registered)
    {
        return new User(id, email, nickname, userSettings, registered);
    }
}