using Domain.Models.PlayerMerge;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Database.Configurations;

internal sealed class PlayerMergeRequestConfiguration : IEntityTypeConfiguration<PlayerMergeRequest>
{
    public void Configure(EntityTypeBuilder<PlayerMergeRequest> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        // Everything asked about one guest, and the inbox query: what is waiting for me to answer.
        builder.HasIndex(x => x.PlayerId);
        builder.HasIndex(x => x.TargetUserId);
    }
}
