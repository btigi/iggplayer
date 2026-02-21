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

    public async Task<int> GetTrackCountAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Tracks.CountAsync();
    }
}