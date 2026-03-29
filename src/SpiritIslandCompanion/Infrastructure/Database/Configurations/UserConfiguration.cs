using Domain.Models.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Database.Configurations;

internal class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(x => x.Value, x => new UserId(x));

        builder.OwnsOne(x => x.Email, b =>
        {
            b.Property(e => e.Value)
                .HasColumnName("Email")
                .HasMaxLength(256)
                .IsRequired();

            b.HasIndex(e => e.Value)
                .IsUnique();
        });

        builder.OwnsOne(x => x.Nickname);
        builder.OwnsOne(x => x.UserSettings, b =>
        {
            b.OwnsOne(y => y.Id);
            b.OwnsMany(y => y.Expansions);
        });
    }
}
