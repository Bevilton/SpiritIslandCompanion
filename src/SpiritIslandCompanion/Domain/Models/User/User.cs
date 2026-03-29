using Domain.Primitives;

namespace Domain.Models.User;

public class User : AggregateRoot<UserId>
{
    public Email Email { get; private set; }
    public Nickname Nickname { get; private set; }
    public DateTimeOffset Registered { get; private init; }
    public UserSettings UserSettings { get; private set; }

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

    public void UpdateProfile(Nickname nickname)
    {
        Nickname = nickname;
    }

    public void UpdateSettings(UserSettings userSettings)
    {
        UserSettings = userSettings;
    }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    /// <summary>
    /// Empty constructor required for EF Core.
    /// </summary>
    [Obsolete("Empty constructor required for EF Core.")]
    private User(){}
#pragma warning restore
}
