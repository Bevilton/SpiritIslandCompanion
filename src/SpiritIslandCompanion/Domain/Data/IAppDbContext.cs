using Domain.Models.Game;
using Domain.Models.Player;
using Domain.Models.User;
using Microsoft.EntityFrameworkCore;

namespace Domain.Data;

public interface IAppDbContext
{
    public DbSet<Game> Games { get; set; }
    public DbSet<Player> Players { get; set; }
    public DbSet<User> Users { get; set; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}