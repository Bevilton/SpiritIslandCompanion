using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.Demo;

/// <summary>
/// The application's context as the demo sandboxes run it — same model, adjusted only where
/// the SQL Server shape cannot exist on SQLite. The real database never sees this type, so
/// nothing here can drift a migration.
/// </summary>
internal sealed class DemoAppDbContext(DbContextOptions<AppDbContext> options) : AppDbContext(options)
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
}
