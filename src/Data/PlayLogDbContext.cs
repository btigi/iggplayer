using Microsoft.EntityFrameworkCore;

namespace IggPlayer.Data;

public class PlayLogDbContext : DbContext
{
    public PlayLogDbContext(DbContextOptions<PlayLogDbContext> options) : base(options) { }

    public DbSet<PlayLogEntry> PlayHistory => Set<PlayLogEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PlayLogEntry>(e =>
        {
            e.ToTable("play_history");
            e.Property(p => p.Id).HasColumnName("id");
            e.Property(p => p.PlayedAt).HasColumnName("played_at").IsRequired();
            e.Property(p => p.FilePath).HasColumnName("filepath");
            e.Property(p => p.FileName).HasColumnName("filename");
            e.Property(p => p.Title).HasColumnName("title");
            e.Property(p => p.Artist).HasColumnName("artist");
            e.Property(p => p.Album).HasColumnName("album");
            e.Property(p => p.Genre).HasColumnName("genre");
            e.Property(p => p.TrackNumber).HasColumnName("track_number");
            e.Property(p => p.Year).HasColumnName("year");
            e.Property(p => p.DurationMs).HasColumnName("duration_ms");
        });
    }
}