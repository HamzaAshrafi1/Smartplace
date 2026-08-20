namespace SmartPlace.API.Models;

public class Skill
{
    public int SkillId { get; set; }

    public string Name { get; set; } = string.Empty;

    public ICollection<StudentSkill> StudentSkills { get; set; }
        = new List<StudentSkill>();
}