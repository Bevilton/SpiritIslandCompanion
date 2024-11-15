using Domain.Models.Static;
using Domain.Primitives;

namespace Domain.Models.User;

public class UserSettings : Entity<UserSettingsId>
{
    public IReadOnlyCollection<ExpansionId> Expansions => _expansions.AsReadOnly();
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