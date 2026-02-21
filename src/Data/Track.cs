namespace IggPlayer.Data;

public class Track
{
    public int Id { get; set; }
    public required string FileName { get; set; }
    public string? Title { get; set; }
    public string? Artist { get; set; }
    public string? Album { get; set; }
    public string? Genre { get; set; }
    public string? TrackNumber { get; set; }
    public string? Year { get; set; }
    public TimeSpan Duration { get; set; }

    public required string RelativePath { get; set; }

    public string DisplayName => !string.IsNullOrWhiteSpace(Title)
        ? !string.IsNullOrWhiteSpace(Artist) ? $"{Artist} - {Title}" : Title
        : FileName;
}