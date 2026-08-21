using Domain.Models.User;
using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.Demo;

/// <summary>
/// The application's context as the demo sandboxes run it — same model, adjusted only where
/// the SQL Server shape cannot exist on SQLite. The real database never sees this type, so
/// nothing here can drift a migration.
/// </summary>
public sealed class DemoAppDbContext(DbContextOptions<AppDbContext> options) : AppDbContext(options)
{
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        // SQLite stores DateTimeOffset as text and refuses to ORDER BY it (see the provider's
        // NotSupportedException in any query that sorts by StartedAt). Binary-encoded it is an
        // integer and sorts in SQL, offsets preserved.
        configurationBuilder.Properties<DateTimeOffset>()
            .HaveConversion<DateTimeOffsetToBinaryConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // The owned collection of a user's expansions keys itself by convention on a
        // synthesized identity ordinal inside a composite primary key. SQL Server generates
        // that; SQLite can only auto-generate a single-column integer key, so inserts arrive
        // with no value and fail. Re-key the rows on the expansion id itself — a user owns an
        // expansion once, so the value is naturally unique per owner — and drop the ordinal.
        var expansions = modelBuilder.Entity<User>()
            .OwnsOne(u => u.UserSettings)
            .OwnsMany(s => s.Expansions);
        expansions.HasKey("UserSettingsUserId", nameof(Domain.Models.Static.ExpansionId.Value));
        if (expansions.OwnedEntityType.FindProperty("Id") is { } ordinal)
            expansions.OwnedEntityType.RemoveProperty(ordinal);
    }
}
