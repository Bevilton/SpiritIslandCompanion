using Domain.Primitives;

namespace Domain.Models.User;

public class User : AggregateRoot<UserId>
{
    public Email Email { get; private set; }

    /// <summary>
    /// The name the user chose for themselves, or null until they have. Deliberately not
    /// seeded from the identity provider: a name taken from OIDC claims is the provider's
    /// guess, and treating it as chosen would mean the account never gets asked. Read
    /// <see cref="DisplayName"/> rather than this — nothing in the app should print a raw
    /// nickname and leave the unnamed case to the caller.
    /// </summary>
    public Nickname? Nickname { get; private set; }

    /// <summary>
    /// What this account is called wherever a person is named. The nickname once set,
    /// the email address until then — an address is a poor name but it is at least one
    /// the people who know this account will recognise.
    /// </summary>
    public string DisplayName => Nickname?.Value ?? Email.Value;

    public DateTimeOffset Registered { get; private init; }
    public UserSettings UserSettings { get; private set; }

    private User(UserId id, Email email, Nickname? nickname, UserSettings userSettings, DateTimeOffset registered) : base(id)
    {
        Email = email;
        Nickname = nickname;
        UserSettings = userSettings;
        Registered = registered;
    }

    public static User Create(UserId id, Email email, Nickname? nickname, UserSettings userSettings, DateTimeOffset registered)
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
