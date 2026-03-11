using IggPlayer.Data;
using Microsoft.EntityFrameworkCore;

namespace IggPlayer.Services;

public class MusicService
{
    private readonly IDbContextFactory<MusicDbContext> _dbFactory;

    public MusicService(IDbContextFactory<MusicDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<Track>> SearchAsync(string query, int maxResults = 50)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        if (string.IsNullOrWhiteSpace(query))
            return await db.Tracks.OrderBy(t => t.Artist).ThenBy(t => t.Title).Take(maxResults).ToListAsync();

        var pattern = $"%{query}%";
        return await db.Tracks
            .Where(t => EF.Functions.Like(t.Title!, pattern)
                     || EF.Functions.Like(t.Artist!, pattern)
                     || EF.Functions.Like(t.FileName, pattern)
                     || EF.Functions.Like(t.Album!, pattern))
            .OrderBy(t => t.Artist).ThenBy(t => t.Title)
            .Take(maxResults)
            .ToListAsync();
    }

    public async Task<Track?> GetByIdAsync(int id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Tracks.FindAsync(id);
    }

    public async Task<List<Track>> GetTracksByIdsAsync(IReadOnlyList<int> ids)
    {
        if (ids.Count == 0) return [];
        await using var db = await _dbFactory.CreateDbContextAsync();
        var idSet = ids.Distinct().ToList();
        var tracks = await db.Tracks.Where(t => idSet.Contains(t.Id)).ToListAsync();
        return ids.Select(id => tracks.FirstOrDefault(t => t.Id == id)).Where(t => t is not null).Cast<Track>().ToList();
    }

    public async Task<int> GetTrackCountAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Tracks.CountAsync();
    }

    public async Task<List<Track>> GetTracksByArtistAsync(string artist, int maxResults = 500)
    {
        if (string.IsNullOrWhiteSpace(artist))
            return [];
        await using var db = await _dbFactory.CreateDbContextAsync();
        var pattern = $"%{artist}%";
        return await db.Tracks
            .Where(t => EF.Functions.Like(t.Artist!, pattern))
            .OrderBy(t => t.Album).ThenBy(t => t.TrackNumber).ThenBy(t => t.Title)
            .Take(maxResults)
            .ToListAsync();
    }

    public async Task<List<Track>> GetTracksByAlbumAsync(string album, int maxResults = 500)
    {
        if (string.IsNullOrWhiteSpace(album))
            return [];
        await using var db = await _dbFactory.CreateDbContextAsync();
        var pattern = $"%{album}%";
        return await db.Tracks
            .Where(t => EF.Functions.Like(t.Album!, pattern))
            .OrderBy(t => t.TrackNumber).ThenBy(t => t.Title)
            .Take(maxResults)
            .ToListAsync();
    }
}