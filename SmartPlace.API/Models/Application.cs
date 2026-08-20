namespace SmartPlace.API.Models;

public class Application
{
    public int ApplicationId { get; set; }

    public DateTime AppliedDate { get; set; } = DateTime.UtcNow;

    public string Status { get; set; } = "Applied";

    public string? Remarks { get; set; }

    // Foreign Key - Student
    public int StudentId { get; set; }

    // Navigation Property - Student
    public Student? Student { get; set; }

    // Foreign Key - Job
    public int JobId { get; set; }

    // Navigation Property - Job
    public Job? Job { get; set; }
}