using SmartPlace.API.Models;

namespace SmartPlace.API.Services;

public class JobMatchingService
{
    public double CalculateMatchScore(
        List<string> studentSkills,
        List<string> requiredSkills)
    {
        if (requiredSkills.Count == 0)
        {
            return 0;
        }

        var normalizedStudentSkills = studentSkills
            .Select(s => s.Trim().ToLower())
            .ToHashSet();

        var normalizedRequiredSkills = requiredSkills
            .Select(s => s.Trim().ToLower())
            .ToList();

        int matchedCount = normalizedRequiredSkills
            .Count(skill => normalizedStudentSkills.Contains(skill));

        double score =
            (double)matchedCount /
            normalizedRequiredSkills.Count *
            100;

        return Math.Round(score, 2);
    }

    public List<string> GetMatchingSkills(
        List<string> studentSkills,
        List<string> requiredSkills)
    {
        var normalizedStudentSkills = studentSkills
            .Select(s => s.Trim().ToLower())
            .ToHashSet();

        return requiredSkills
            .Where(skill =>
                normalizedStudentSkills.Contains(
                    skill.Trim().ToLower()))
            .Distinct()
            .ToList();
    }

    public List<string> GetMissingSkills(
        List<string> studentSkills,
        List<string> requiredSkills)
    {
        var normalizedStudentSkills = studentSkills
            .Select(s => s.Trim().ToLower())
            .ToHashSet();

        return requiredSkills
            .Where(skill =>
                !normalizedStudentSkills.Contains(
                    skill.Trim().ToLower()))
            .Distinct()
            .ToList();
    }
}