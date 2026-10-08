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
            Id = Guid.NewGuid(),
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

    public async Task<ListeningStats> GetListeningStatsAsync(DateTime startUtc, DateTime endExclusiveUtc, int topN = 10)
    {
        var start = startUtc.ToUniversalTime().ToString("o");
        var end = endExclusiveUtc.ToUniversalTime().ToString("o");

        await using var db = await _dbFactory.CreateDbContextAsync();
        var rows = await db.PlayHistory
            .Where(e => string.Compare(e.PlayedAt, start) >= 0 && string.Compare(e.PlayedAt, end) < 0)
            .Select(e => new { e.Title, e.Artist, e.Album, e.Genre, e.Year, e.FileName, e.DurationMs })
            .ToListAsync();

        var entries = rows
            .Select(e => new PlayRow(e.Title, e.Artist, e.Album, e.Genre, e.Year, e.FileName, e.DurationMs))
            .ToList();

        var tracks = TopGrouped(entries, TrackKey, TrackName, e => Clean(e.Artist), topN);
        var albums = TopGrouped(entries, AlbumKey, AlbumName, e => Clean(e.Artist), topN);
        var artists = TopGrouped(entries, e => Clean(e.Artist), e => Clean(e.Artist)!, subtitle: null, topN);
        var genres = TopGrouped(entries, e => Clean(e.Genre), e => Clean(e.Genre)!, subtitle: null, int.MaxValue);

        return new ListeningStats
        {
            TotalPlays = entries.Count,
            TotalDurationMs = entries.Sum(e => (long)(e.DurationMs ?? 0)),
            UniqueTracks = CountKeys(entries, TrackKey),
            UniqueAlbums = CountKeys(entries, AlbumKey),
            UniqueArtists = CountKeys(entries, e => Clean(e.Artist)),
            TopTracks = tracks,
            TopAlbums = albums,
            TopArtists = artists,
            TopGenres = genres,
            DecadeBreakdown = DecadeBuckets(entries)
        };
    }

    private sealed record PlayRow(
        string? Title,
        string? Artist,
        string? Album,
        string? Genre,
        string? Year,
        string? FileName,
        int? DurationMs);

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? TrackKey(PlayRow e)
    {
        var name = Clean(e.Title) ?? Clean(e.FileName);
        if (name is null) return null;
        return $"{name}\u001f{Clean(e.Artist) ?? ""}";
    }

    private static string TrackName(PlayRow e) => Clean(e.Title) ?? Clean(e.FileName)!;

    private static string? AlbumKey(PlayRow e)
    {
        var album = Clean(e.Album);
        if (album is null) return null;
        return $"{album}\u001f{Clean(e.Artist) ?? ""}";
    }

    private static string AlbumName(PlayRow e) => Clean(e.Album)!;

    private static int CountKeys(List<PlayRow> entries, Func<PlayRow, string?> key) =>
        entries.Select(key).Where(k => k is not null).Distinct(StringComparer.OrdinalIgnoreCase).Count();

    private static List<StatItem> TopGrouped(
        List<PlayRow> entries,
        Func<PlayRow, string?> key,
        Func<PlayRow, string> name,
        Func<PlayRow, string?>? subtitle,
        int topN)
    {
        return entries
            .Select(e => (Key: key(e), Name: key(e) is null ? null : name(e), Sub: subtitle?.Invoke(e)))
            .Where(x => x.Key is not null && x.Name is not null)
            .GroupBy(x => x.Key!, StringComparer.OrdinalIgnoreCase)
            .Select(g => new StatItem
            {
                Name = g.First().Name!,
                Subtitle = g.Select(x => x.Sub).FirstOrDefault(s => !string.IsNullOrEmpty(s)),
                PlayCount = g.Count()
            })
            .OrderByDescending(s => s.PlayCount)
            .ThenBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .Take(topN)
            .ToList();
    }

    private static List<StatItem> DecadeBuckets(List<PlayRow> entries)
    {
        return entries
            .Select(e => ParseYear(e.Year))
            .Where(y => y is not null)
            .Select(y => (y!.Value / 10) * 10)
            .GroupBy(d => d)
            .Select(g => new StatItem { Name = $"{g.Key}s", PlayCount = g.Count() })
            .OrderBy(s => s.Name, StringComparer.Ordinal)
            .ToList();
    }

    private static int? ParseYear(string? year)
    {
        if (string.IsNullOrWhiteSpace(year)) return null;
        var digits = new string(year.Trim().TakeWhile(char.IsDigit).ToArray());
        if (digits.Length >= 4 && int.TryParse(digits[..4], out var y) && y is >= 1900 and <= 2100)
            return y;
        return null;
    }
}