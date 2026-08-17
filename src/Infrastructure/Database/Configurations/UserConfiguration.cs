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

        // Optional: an account has no name of its own until it chooses one — see User.Nickname.
        builder.OwnsOne(x => x.Nickname, b =>
        {
            b.Property(n => n.Value)
                .HasMaxLength(Nickname.MaxLength)
                .IsRequired(false);
        });
        builder.Navigation(x => x.Nickname).IsRequired(false);

        builder.OwnsOne(x => x.UserSettings, b =>
        {
            b.OwnsOne(y => y.Id);
            b.OwnsMany(y => y.Expansions);
        });
    }
}
