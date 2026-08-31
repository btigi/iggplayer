using IggPlayer.Data;
using Microsoft.EntityFrameworkCore;

namespace IggPlayer.Services;

public class LibraryScanService
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3",
        ".flac"
    };

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

        var existingPaths = await db.Tracks.Select(t => t.RelativePath).ToListAsync(ct);
        var existing = new HashSet<string>(existingPaths, StringComparer.OrdinalIgnoreCase);

        int added = 0, skipped = 0, failed = 0;
        var pendingSinceSave = 0;

        try
        {
            foreach (var fullPath in EnumerateAudioFiles(_basePath))
            {
                ct.ThrowIfCancellationRequested();

                var relativePath = Path.GetRelativePath(_basePath, fullPath);

                if (existing.Contains(relativePath))
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
                    existing.Add(relativePath);
                    added++;
                    pendingSinceSave++;

                    if (pendingSinceSave >= 25)
                    {
                        await db.SaveChangesAsync(ct);
                        pendingSinceSave = 0;
                        _logger.LogInformation("Scan progress: {Added} added, {Skipped} skipped, {Failed} failed", added, skipped, failed);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to read tags from {Path}", fullPath);
                    failed++;
                }
            }
        }
        finally
        {
            if (pendingSinceSave > 0)
            {
                try
                {
                    await db.SaveChangesAsync(CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to flush {Pending} pending tracks at end of scan", pendingSinceSave);
                    throw;
                }
            }
        }

        _logger.LogInformation("Scan complete: {Added} added, {Skipped} skipped, {Failed} failed", added, skipped, failed);
        return (added, skipped, failed);
    }

    private IEnumerable<string> EnumerateAudioFiles(string root)
    {
        var dirs = new Stack<string>();
        dirs.Push(root);

        while (dirs.Count > 0)
        {
            var dir = dirs.Pop();

            IEnumerable<string> subdirs;
            try
            {
                subdirs = Directory.EnumerateDirectories(dir);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to list directories under {Path}", dir);
                continue;
            }

            foreach (var subdir in subdirs)
                dirs.Push(subdir);

            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(dir);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to list files under {Path}", dir);
                continue;
            }

            foreach (var file in files)
            {
                if (SupportedExtensions.Contains(Path.GetExtension(file)))
                    yield return file;
            }
        }
    }
}
