namespace SmartPlace.API.Models;

public class Student
{
    public int StudentId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public decimal CGPA { get; set; }

    public int Backlogs { get; set; }

    public int GraduationYear { get; set; }

    // Foreign Key
    public int DepartmentId { get; set; }

    // Navigation Property
    public Department? Department { get; set; }

    // Many-to-Many relationship with Skill
    public ICollection<StudentSkill> StudentSkills { get; set; }
        = new List<StudentSkill>();

    // One Student can have many Applications
    public ICollection<Application> Applications { get; set; }
        = new List<Application>();
}