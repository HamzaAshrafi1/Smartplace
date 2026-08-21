namespace SmartPlace.API.Models;

public class Student
{
    public int StudentId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public decimal CGPA { get; set; }

    public int Backlogs { get; set; }

    public int GraduationYear { get; set; }

    // --------------------------------------------------
    // IDENTITY USER LINK
    // --------------------------------------------------

    // Nullable because older students may not yet
    // be linked to an Identity account.
    public string? ApplicationUserId { get; set; }

    public ApplicationUser? ApplicationUser { get; set; }

    // --------------------------------------------------
    // DEPARTMENT
    // --------------------------------------------------

    public int DepartmentId { get; set; }

    public Department? Department { get; set; }

    // --------------------------------------------------
    // SKILLS
    // --------------------------------------------------

    public ICollection<StudentSkill> StudentSkills { get; set; }
        = new List<StudentSkill>();

    // --------------------------------------------------
    // APPLICATIONS
    // --------------------------------------------------

    public ICollection<Application> Applications { get; set; }
        = new List<Application>();

    // --------------------------------------------------
    // PLACEMENT
    // --------------------------------------------------

    public Placement? Placement { get; set; }

    // --------------------------------------------------
    // RESUME
    // --------------------------------------------------

    public Resume? Resume { get; set; }
}