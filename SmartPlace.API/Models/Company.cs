namespace SmartPlace.API.Models;

public class Company
{
    public int CompanyId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Industry { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public string Website { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string ApprovalStatus { get; set; } = "Pending";

    // Recruiter ownership
    public string? RecruiterUserId { get; set; }

    public ApplicationUser? RecruiterUser { get; set; }

    public ICollection<Job> Jobs { get; set; }
        = new List<Job>();

    public ICollection<Placement> Placements { get; set; }
        = new List<Placement>();
}