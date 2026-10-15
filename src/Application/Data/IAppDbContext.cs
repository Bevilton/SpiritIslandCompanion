using Domain.Models.Friendship;
using Domain.Models.Game;
using Domain.Models.IslandLayout;
using Domain.Models.Player;
using Domain.Models.PlayerMerge;
using Domain.Models.User;
using Microsoft.EntityFrameworkCore;

namespace Application.Data;

public interface IAppDbContext
{
    DbSet<Game> Games { get; }
    DbSet<Player> Players { get; }
    DbSet<User> Users { get; }
    DbSet<Friendship> Friendships { get; }
    DbSet<PlayerMergeRequest> PlayerMergeRequests { get; }
    DbSet<CustomIslandLayout> CustomIslandLayouts { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Forgets everything tracked, saved or not, so one command leaves nothing behind for the
    /// next one in the same scope — neither half-made changes to save by accident, nor tracked
    /// instances to read back stale (see <c>SaveChangesBehaviour</c>).
    /// </summary>
    void DiscardChanges();
}
