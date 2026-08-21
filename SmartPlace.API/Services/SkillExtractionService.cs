namespace SmartPlace.API.Services;

public class SkillExtractionService
{
    private static readonly List<string> KnownSkills =
    [
        "C",
        "C++",
        "C#",
        "Java",
        "Python",
        "JavaScript",
        "TypeScript",
        "ASP.NET",
        ".NET",
        "SQL",
        "MySQL",
        "SQL Server",
        "MongoDB",
        "HTML",
        "CSS",
        "Bootstrap",
        "React",
        "Angular",
        "Node.js",
        "Git",
        "GitHub",
        "Docker",
        "Kubernetes",
        "Linux",
        "AWS",
        "Azure",
        "Machine Learning",
        "Artificial Intelligence",
        "Cyber Security",
        "Networking",
        "Penetration Testing",
        "Ethical Hacking",
        "Spring Boot",
        "REST API"
    ];

    public List<string> ExtractSkills(string text)
    {
        var detectedSkills = new List<string>();

        if (string.IsNullOrWhiteSpace(text))
        {
            return detectedSkills;
        }

        foreach (var skill in KnownSkills)
        {
            if (text.Contains(
                skill,
                StringComparison.OrdinalIgnoreCase))
            {
                detectedSkills.Add(skill);
            }
        }

        return detectedSkills
            .Distinct()
            .OrderBy(s => s)
            .ToList();
    }
}