using IggPlayer.Data;
using Microsoft.EntityFrameworkCore;

namespace IggPlayer.Services;

public class PlayLogService
{
    private readonly IDbContextFactory<PlayLogDbContext> _dbFactory;
    private readonly FileStreamService _fileService;

    public PlayLogService(IDbContextFactory<PlayLogDbContext> dbFactory, FileStreamService fileService)
    {
        _dbFactory = dbFactory;
        _fileService = fileService;
    }

    public async Task LogPlayAsync(Track track)
    {
        string? resolvedPath = null;
        try
        {
            resolvedPath = _fileService.ResolvePath(track.RelativePath);
        }
        catch { }

        await using var db = await _dbFactory.CreateDbContextAsync();
        db.PlayHistory.Add(new PlayLogEntry
        {
            PlayedAt = DateTime.UtcNow.ToString("o"),
            FilePath = resolvedPath,
            FileName = track.FileName,
            Title = track.Title,
            Artist = track.Artist,
            Album = track.Album,
            Genre = track.Genre,
            TrackNumber = track.TrackNumber,
            Year = track.Year,
            DurationMs = (int)track.Duration.TotalMilliseconds
        });
        await db.SaveChangesAsync();
    }

    public async Task<List<PlayLogEntry>> GetRecentPlaysAsync(int count = 20)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.PlayHistory
            .OrderByDescending(e => e.PlayedAt)
            .Take(count)
            .ToListAsync();
    }
}