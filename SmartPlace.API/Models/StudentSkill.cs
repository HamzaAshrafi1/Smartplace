namespace SmartPlace.API.Models;

public class StudentSkill
{
    public int StudentId { get; set; }

    public Student Student { get; set; } = null!;

    public int SkillId { get; set; }

    public Skill Skill { get; set; } = null!;
}