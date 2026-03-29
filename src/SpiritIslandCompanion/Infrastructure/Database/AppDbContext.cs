using Application.Data;
using Domain.Models.Friendship;
using Domain.Models.Game;
using Domain.Models.Player;
using Domain.Models.User;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Database;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<Game> Games { get; set; } = null!;
    public DbSet<Player> Players { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Friendship> Friendships { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
