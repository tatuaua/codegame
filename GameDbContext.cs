using Microsoft.EntityFrameworkCore;

public class GameDbContext : DbContext
{
    public DbSet<Player> Players { get; set; }
    public DbSet<GameBase> Games { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql("Host=your_host;Database=your_db;Username=your_user;Password=your_password");
    }
}
