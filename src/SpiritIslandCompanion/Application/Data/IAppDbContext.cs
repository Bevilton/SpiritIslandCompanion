using Domain.Models.Friendship;
using Domain.Models.Game;
using Domain.Models.Player;
using Domain.Models.User;
using Microsoft.EntityFrameworkCore;

namespace Application.Data;

public interface IAppDbContext
{
    public DbSet<Game> Games { get; set; }
    public DbSet<Player> Players { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Friendship> Friendships { get; set; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
