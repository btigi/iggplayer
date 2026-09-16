using IggPlayer.Data;
using Microsoft.EntityFrameworkCore;

namespace IggPlayer.Services;

public class MusicService
{
    private readonly IDbContextFactory<MusicDbContext> _dbFactory;
    private readonly HashSet<string> _hideArtistsOnAlbums;

    public MusicService(IDbContextFactory<MusicDbContext> dbFactory, IConfiguration configuration)
    {
        _dbFactory = dbFactory;
        _hideArtistsOnAlbums = configuration.GetSection("HideArtistsOnAlbums")
            .Get<List<string>>()?
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .ToHashSet(StringComparer.OrdinalIgnoreCase)
            ?? [];
    }

    public async Task<List<Track>> SearchAsync(string query)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        if (string.IsNullOrWhiteSpace(query))
            return await db.Tracks.OrderBy(t => t.Artist).ThenBy(t => t.Title).ToListAsync();

        var pattern = $"%{query}%";
        return await db.Tracks
            .Where(t => EF.Functions.Like(t.Title!, pattern)
                     || EF.Functions.Like(t.Artist!, pattern)
                     || EF.Functions.Like(t.FileName, pattern)
                     || EF.Functions.Like(t.Album!, pattern))
            .OrderBy(t => t.Artist).ThenBy(t => t.Title)
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

    public async Task<List<AlbumInfo>> GetAlbumsAsync(string query = "")
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var tracks = db.Tracks.Where(t => t.Album != null && t.Album != "");

        if (!string.IsNullOrWhiteSpace(query))
        {
            var pattern = $"%{query}%";
            tracks = tracks.Where(t => EF.Functions.Like(t.Album!, pattern));
        }

        var rows = await tracks
            .Select(t => new { t.Album, t.Artist, t.RelativePath })
            .ToListAsync();

        return GroupAlbums(rows.Select(t => (t.Album, t.Artist, t.RelativePath)));
    }

    public async Task<List<string>> GetArtistsAsync(string query = "")
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var artists = db.Tracks
            .Where(t => t.Artist != null && t.Artist != "")
            .Select(t => t.Artist!);

        if (!string.IsNullOrWhiteSpace(query))
        {
            var pattern = $"%{query}%";
            artists = artists.Where(artist => EF.Functions.Like(artist, pattern));
        }

        var result = await artists
            .Distinct()
            .OrderBy(artist => artist)
            .ToListAsync();

        if (_hideArtistsOnAlbums.Count == 0)
            return result;

        var hiddenArtists = await db.Tracks
            .Where(t => t.Artist != null && t.Artist != ""
                     && t.Album != null && _hideArtistsOnAlbums.Contains(t.Album))
            .Select(t => t.Artist!)
            .Distinct()
            .ToListAsync();

        var hiddenSet = hiddenArtists.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return result.Where(a => !hiddenSet.Contains(a)).ToList();
    }

    public async Task<List<AlbumInfo>> GetAlbumsByArtistAsync(string artist, string query = "")
    {
        if (string.IsNullOrWhiteSpace(artist))
            return [];

        await using var db = await _dbFactory.CreateDbContextAsync();
        var tracks = db.Tracks
            .Where(t => t.Artist == artist && t.Album != null && t.Album != "");

        if (!string.IsNullOrWhiteSpace(query))
        {
            var pattern = $"%{query}%";
            tracks = tracks.Where(t => EF.Functions.Like(t.Album!, pattern));
        }

        var rows = await tracks
            .Select(t => new { t.Album, t.Artist, t.RelativePath })
            .ToListAsync();

        return GroupAlbums(rows.Select(t => (t.Album, t.Artist, t.RelativePath)));
    }

    public async Task<List<Track>> GetTracksByArtistAsync(string artist)
    {
        if (string.IsNullOrWhiteSpace(artist))
            return [];
        await using var db = await _dbFactory.CreateDbContextAsync();
        var pattern = $"%{artist}%";
        return await db.Tracks
            .Where(t => EF.Functions.Like(t.Artist!, pattern))
            .OrderBy(t => t.Album).ThenBy(t => t.TrackNumber).ThenBy(t => t.Title)
            .ToListAsync();
    }

    public async Task<List<Track>> GetTracksByAlbumAsync(string folder)
    {
        folder = NormalizeFolder(folder);
        await using var db = await _dbFactory.CreateDbContextAsync();

        IQueryable<Track> tracks;
        if (folder.Length == 0)
        {
            tracks = db.Tracks.Where(t => t.RelativePath == t.FileName);
        }
        else
        {
            var posixFolder = folder.Replace('\\', '/');
            tracks = db.Tracks.Where(t =>
                t.RelativePath == folder + "\\" + t.FileName
                || t.RelativePath == posixFolder + "/" + t.FileName);
        }

        return await tracks
            .OrderBy(t => t.TrackNumber).ThenBy(t => t.Title)
            .ToListAsync();
    }

    public static string GetAlbumFolder(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath))
            return "";

        var normalized = relativePath.Replace('/', '\\').TrimEnd('\\');
        var lastSlash = normalized.LastIndexOf('\\');
        return lastSlash < 0 ? "" : normalized[..lastSlash];
    }

    private static string NormalizeFolder(string? folder)
        => string.IsNullOrEmpty(folder) ? "" : folder.Replace('/', '\\').TrimEnd('\\');

    private static List<AlbumInfo> GroupAlbums(IEnumerable<(string? Album, string? Artist, string RelativePath)> tracks)
        => tracks
            .GroupBy(t => GetAlbumFolder(t.RelativePath), StringComparer.OrdinalIgnoreCase)
            .Select(g => new AlbumInfo(
                MostCommon(g.Select(t => t.Album)) ?? g.Key,
                MostCommon(g.Select(t => t.Artist)),
                g.Key))
            .OrderBy(album => album.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(album => album.Artist, StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static string? MostCommon(IEnumerable<string?> values)
        => values
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .GroupBy(v => v!, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .FirstOrDefault();
}