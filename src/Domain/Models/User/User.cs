using Domain.Errors;
using Domain.Models.Static;
using Domain.Primitives;
using Domain.Results;

namespace Domain.Models.User;

public class User : AggregateRoot<UserId>
{
    public Email Email { get; private init; }

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

    /// <summary>
    /// The expansions the player has — what every picker in the app is narrowed to. Always
    /// includes the base game: you cannot play Spirit Island without it, and nowhere in the app
    /// is it offered as a choice.
    /// <para>
    /// Guaranteed here rather than trusted from the caller, because the whole app reads this to
    /// decide what to offer and an account that has never opened Settings has nothing stored —
    /// read literally, that account owns nothing and every picker comes up empty.
    /// </para>
    /// <para>
    /// Derived on read, not fixed up on write: EF materialises straight into the backing field,
    /// so a row already stored without the base game has to read correctly too.
    /// </para>
    /// </summary>
    public IReadOnlyCollection<ExpansionId> OwnedExpansions =>
        _ownedExpansions.Contains(Static.Data.Expansions.BaseGame)
            ? _ownedExpansions.AsReadOnly()
            : _ownedExpansions.Prepend(Static.Data.Expansions.BaseGame).ToList().AsReadOnly();

    /// <summary>Nothing stored yet — the shelf of an account that has never opened Settings.</summary>
    private List<ExpansionId> _ownedExpansions = [];

    private User(UserId id, Email email, Nickname? nickname, DateTimeOffset registered) : base(id)
    {
        Email = email;
        Nickname = nickname;
        Registered = registered;
    }

    /// <summary>
    /// A fresh account: named or not yet, and owning nothing beyond the base game until it says
    /// otherwise through <see cref="SetOwnedExpansions"/>.
    /// </summary>
    public static User Create(UserId id, Email email, Nickname? nickname, DateTimeOffset registered) =>
        new(id, email, nickname, registered);

    /// <summary>Names the account — or renames it, which is the same act.</summary>
    public void SetNickname(Nickname nickname) => Nickname = nickname;

    /// <summary>
    /// Replaces the shelf. Every id has to name a catalogue entry, and an expansion is owned
    /// once — both settled here rather than trusted from the caller, so what is stored can
    /// never say otherwise.
    /// </summary>
    public Result SetOwnedExpansions(IEnumerable<ExpansionId> expansions)
    {
        var distinct = expansions.Distinct().ToList();
        if (!distinct.All(Static.Data.Expansions.Includes))
            return Result.Failure(DomainErrors.User.UnknownExpansion);

        _ownedExpansions = distinct;
        return Result.Success();
    }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    /// <summary>
    /// Empty constructor required for EF Core.
    /// </summary>
    [Obsolete("Empty constructor required for EF Core.")]
    private User()
    {
    }
#pragma warning restore
}
