using System.ComponentModel.DataAnnotations.Schema;

namespace IggPlayer.Data;

[Table("play_history")]
public class PlayLogEntry
{
    [Column("id")]
    public int Id { get; set; }

    [Column("played_at")]
    public required string PlayedAt { get; set; }

    [Column("filepath")]
    public string? FilePath { get; set; }

    [Column("filename")]
    public string? FileName { get; set; }

    [Column("title")]
    public string? Title { get; set; }

    [Column("artist")]
    public string? Artist { get; set; }

    [Column("album")]
    public string? Album { get; set; }

    [Column("genre")]
    public string? Genre { get; set; }

    [Column("track_number")]
    public string? TrackNumber { get; set; }

    [Column("year")]
    public string? Year { get; set; }

    [Column("duration_ms")]
    public int? DurationMs { get; set; }
}