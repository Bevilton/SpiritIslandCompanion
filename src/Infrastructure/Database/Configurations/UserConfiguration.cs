using Domain.Models.Static;
using Domain.Models.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Database.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.Email).IsUnique();

        // The expansions are one JSON array on the user's row: a handful of short ids nobody
        // queries into, read and written with the user. Through the backing field, so the
        // read-side guarantee in User.OwnedExpansions (the base game is always there) never
        // leaks into storage.
        builder.PrimitiveCollection(x => x.OwnedExpansions)
            .HasField("_ownedExpansions")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("Expansions")
            .ElementType()
            .HasConversion<ValueObjectConverter<ExpansionId, string>>();
    }
}
