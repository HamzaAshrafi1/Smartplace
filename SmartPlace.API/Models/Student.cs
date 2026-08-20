namespace SmartPlace.API.Models;

public class Student
{
    public int StudentId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public decimal CGPA { get; set; }

    public int Backlogs { get; set; }

    public int GraduationYear { get; set; }

    // Department
    public int DepartmentId { get; set; }

    public Department? Department { get; set; }

    // Skills
    public ICollection<StudentSkill> StudentSkills { get; set; }
        = new List<StudentSkill>();

    // Applications
    public ICollection<Application> Applications { get; set; }
        = new List<Application>();

    // Placement
    public Placement? Placement { get; set; }

    // Resume
    public Resume? Resume { get; set; }
}