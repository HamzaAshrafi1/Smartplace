namespace SmartPlace.API.Models;

public class Job
{
    public int JobId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal Package { get; set; }

    public decimal MinimumCGPA { get; set; }

    public int MaximumBacklogs { get; set; }

    public int GraduationYear { get; set; }

    public string Location { get; set; } = string.Empty;

    public string EmploymentType { get; set; } = "Full-Time";

    public DateTime PostedDate { get; set; } = DateTime.UtcNow;

    public DateTime? ApplicationDeadline { get; set; }

    public string Status { get; set; } = "Pending";

    // Foreign Key
    public int CompanyId { get; set; }

    // Navigation Property
    public Company? Company { get; set; }

    // One Job can have many Applications
    public ICollection<Application> Applications { get; set; }
        = new List<Application>();
}