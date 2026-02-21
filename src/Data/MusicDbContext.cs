using Microsoft.EntityFrameworkCore;

namespace IggPlayer.Data;

public class MusicDbContext : DbContext
{
    public MusicDbContext(DbContextOptions<MusicDbContext> options) : base(options) { }

    public DbSet<Track> Tracks => Set<Track>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Track>(e =>
        {
            e.HasIndex(t => t.RelativePath).IsUnique();
            e.HasIndex(t => t.Title);
            e.HasIndex(t => t.Artist);
            e.HasIndex(t => t.FileName);
        });
    }
}