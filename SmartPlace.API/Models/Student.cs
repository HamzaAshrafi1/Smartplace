namespace SmartPlace.API.Models;

public class Student
{
    public int StudentId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    // Academic history
    public decimal TenthPercentage { get; set; }

    public decimal TwelfthPercentage { get; set; }

    public decimal CGPA { get; set; }

    public int Backlogs { get; set; }

    public int GraduationYear { get; set; }

    // Identity ownership
    public string? ApplicationUserId { get; set; }

    public ApplicationUser? ApplicationUser { get; set; }

    // Department / Branch
    public int DepartmentId { get; set; }

    public Department? Department { get; set; }

    public ICollection<StudentSkill> StudentSkills { get; set; }
        = new List<StudentSkill>();

    public ICollection<Application> Applications { get; set; }
        = new List<Application>();

    public Placement? Placement { get; set; }

    public Resume? Resume { get; set; }
}