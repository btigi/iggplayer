using IggPlayer.Data;
using Microsoft.EntityFrameworkCore;

namespace IggPlayer.Services;

public class LibraryScanService
{
    private readonly IDbContextFactory<MusicDbContext> _dbFactory;
    private readonly string _basePath;
    private readonly ILogger<LibraryScanService> _logger;

    public LibraryScanService(
        IDbContextFactory<MusicDbContext> dbFactory,
        IConfiguration configuration,
        ILogger<LibraryScanService> logger)
    {
        _dbFactory = dbFactory;
        _basePath = configuration["MusicFilesBasePath"] ?? throw new InvalidOperationException("MusicFilesBasePath is not configured.");
        _logger = logger;
    }

    public async Task<(int added, int skipped, int failed)> ScanAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var existingPaths = await db.Tracks.Select(t => t.RelativePath).ToHashSetAsync(ct);

        var mp3Files = Directory.EnumerateFiles(_basePath, "*.mp3", SearchOption.AllDirectories);

        int added = 0, skipped = 0, failed = 0;

        foreach (var fullPath in mp3Files)
        {
            ct.ThrowIfCancellationRequested();

            var relativePath = Path.GetRelativePath(_basePath, fullPath);

            if (existingPaths.Contains(relativePath))
            {
                skipped++;
                continue;
            }

            try
            {
                using var tagFile = TagLib.File.Create(fullPath);
                var tag = tagFile.Tag;
                var track = new Track
                {
                    FileName = Path.GetFileName(fullPath),
                    Title = string.IsNullOrWhiteSpace(tag.Title) ? Path.GetFileNameWithoutExtension(fullPath) : tag.Title,
                    Artist = tag.FirstPerformer,
                    Album = tag.Album,
                    Genre = tag.FirstGenre,
                    TrackNumber = tag.Track > 0 ? tag.Track.ToString() : null,
                    Year = tag.Year > 0 ? tag.Year.ToString() : null,
                    Duration = tagFile.Properties.Duration,
                    RelativePath = relativePath
                };

                db.Tracks.Add(track);
                added++;

                if (added % 100 == 0)
                {
                    await db.SaveChangesAsync(ct);
                    _logger.LogInformation("Scan progress: {Added} added, {Skipped} skipped", added, skipped);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read tags from {Path}", fullPath);
                failed++;
            }
        }

        await db.SaveChangesAsync(ct);
        _logger.LogInformation("Scan complete: {Added} added, {Skipped} skipped, {Failed} failed", added, skipped, failed);

        return (added, skipped, failed);
    }
}