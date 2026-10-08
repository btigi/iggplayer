namespace IggPlayer.Data;

public class StatItem
{
    public required string Name { get; init; }
    public string? Subtitle { get; init; }
    public int PlayCount { get; init; }
}

public class ListeningStats
{
    public int TotalPlays { get; init; }
    public long TotalDurationMs { get; init; }
    public int UniqueTracks { get; init; }
    public int UniqueAlbums { get; init; }
    public int UniqueArtists { get; init; }
    public List<StatItem> TopTracks { get; init; } = [];
    public List<StatItem> TopAlbums { get; init; } = [];
    public List<StatItem> TopArtists { get; init; } = [];
    public List<StatItem> TopGenres { get; init; } = [];
    public List<StatItem> DecadeBreakdown { get; init; } = [];
}
