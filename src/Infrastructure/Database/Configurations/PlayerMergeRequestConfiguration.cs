using Domain.Models.PlayerMerge;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Database.Configurations;

internal class PlayerMergeRequestConfiguration : IEntityTypeConfiguration<PlayerMergeRequest>
{
    public void Configure(EntityTypeBuilder<PlayerMergeRequest> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(x => x.Value, x => new PlayerMergeRequestId(x));

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.OwnsOne(x => x.PlayerId, b =>
        {
            b.Property(e => e.Value)
                .HasColumnName("PlayerId")
                .IsRequired();

            b.HasIndex(e => e.Value);
        });

        builder.OwnsOne(x => x.RequesterId, b =>
        {
            b.Property(e => e.Value)
                .HasColumnName("RequesterId")
                .IsRequired();
        });

        builder.OwnsOne(x => x.TargetUserId, b =>
        {
            b.Property(e => e.Value)
                .HasColumnName("TargetUserId")
                .IsRequired();

            // The inbox query: what is waiting for me to answer.
            b.HasIndex(e => e.Value);
        });
    }
}
