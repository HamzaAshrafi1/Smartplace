namespace SmartPlace.API.Models;

public class Resume
{
    public int ResumeId { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string FilePath { get; set; } = string.Empty;

    public string ExtractedText { get; set; } = string.Empty;

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public bool IsProcessed { get; set; } = false;

    // Foreign Key
    public int StudentId { get; set; }

    // Navigation Property
    public Student? Student { get; set; }
}