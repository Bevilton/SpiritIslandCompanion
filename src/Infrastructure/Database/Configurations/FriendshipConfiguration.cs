using Domain.Models.Friendship;
using Domain.Models.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Database.Configurations;

internal class FriendshipConfiguration : IEntityTypeConfiguration<Friendship>
{
    public void Configure(EntityTypeBuilder<Friendship> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(x => x.Value, x => new FriendshipId(x));

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.OwnsOne(x => x.RequesterId, b =>
        {
            b.Property(e => e.Value)
                .HasColumnName("RequesterId")
                .IsRequired();
        });

        builder.OwnsOne(x => x.AddresseeId, b =>
        {
            b.Property(e => e.Value)
                .HasColumnName("AddresseeId")
                .IsRequired();

            // Speed up lookups for incoming requests
            b.HasIndex(e => e.Value);
        });
    }
}
