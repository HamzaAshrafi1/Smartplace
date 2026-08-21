#pragma warning disable OPENAI001

using OpenAI.Responses;

namespace SmartPlace.API.Services;

public class OpenAIAnalysisService
{
    private readonly IConfiguration _configuration;

    public OpenAIAnalysisService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<string> GenerateJobMatchExplanation(
        string studentName,
        string jobTitle,
        double matchPercentage,
        List<string> matchingSkills,
        List<string> missingSkills,
        bool academicallyEligible)
    {
        var apiKey = _configuration["OpenAI:ApiKey"];
        var model = _configuration["OpenAI:Model"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "OpenAI API key is missing.");
        }

        if (string.IsNullOrWhiteSpace(model))
        {
            throw new InvalidOperationException(
                "OpenAI model is missing.");
        }

        var client = new ResponsesClient(apiKey);

        var matchingSkillsText =
            matchingSkills.Count > 0
                ? string.Join(", ", matchingSkills)
                : "None";

        var missingSkillsText =
            missingSkills.Count > 0
                ? string.Join(", ", missingSkills)
                : "None";

        var prompt = $"""
        You are an AI career assistant inside the SmartPlace
        college placement management system.

        Analyze the following student-job match.

        Student: {studentName}
        Job: {jobTitle}

        Skill Match Percentage: {matchPercentage}%

        Matching Skills:
        {matchingSkillsText}

        Missing Skills:
        {missingSkillsText}

        Academically Eligible:
        {academicallyEligible}

        Provide a concise analysis containing:

        1. Match Summary
        2. Strengths
        3. Skill Gaps
        4. Recommended Learning Priority
        5. Final Recommendation

        Do not make a recruitment decision.
        Do not invent skills that are not provided.
        Keep the response under 180 words.
        """;

        var response = await client.CreateResponseAsync(
            model,
            prompt);

        return response.Value.GetOutputText();
    }
}

#pragma warning restore OPENAI001