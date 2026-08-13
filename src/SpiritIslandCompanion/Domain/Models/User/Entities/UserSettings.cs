using Domain.Models.Static;
using Domain.Primitives;

namespace Domain.Models.User;

public class UserSettings : Entity<UserSettingsId>
{
    /// <summary>
    /// The expansions the player has. Always includes the base game: you cannot play Spirit
    /// Island without it, and nowhere in the app is it offered as a choice.
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
    public IReadOnlyCollection<ExpansionId> Expansions =>
        _expansions.Contains(Static.Data.Expansions.BaseGame)
            ? _expansions.AsReadOnly()
            : _expansions.Prepend(Static.Data.Expansions.BaseGame).ToList().AsReadOnly();

    private List<ExpansionId> _expansions;

    private UserSettings(UserSettingsId id, List<ExpansionId> expansions) : base(id)
    {
        _expansions = expansions;
    }

    public static UserSettings Create(UserSettingsId id, List<ExpansionId> expansions)
    {
        return new UserSettings(id, expansions);
    }

    public void SetExpansions(List<ExpansionId> expansions)
    {
        _expansions = expansions;
    }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    /// <summary>
    /// Empty constructor required for EF Core.
    /// </summary>
    [Obsolete("Empty constructor required for EF Core.")]
    private UserSettings(){}
#pragma warning restore
}