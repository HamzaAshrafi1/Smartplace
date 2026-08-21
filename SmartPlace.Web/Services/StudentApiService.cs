using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace SmartPlace.Web.Services;

public class StudentApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public StudentApiService(
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor)
    {
        _httpClientFactory = httpClientFactory;
        _httpContextAccessor = httpContextAccessor;
    }

    // --------------------------------------------------
    // CREATE AUTHENTICATED API CLIENT
    // --------------------------------------------------

    private HttpClient CreateClient()
    {
        var client =
            _httpClientFactory.CreateClient("SmartPlaceAPI");

        var token =
            _httpContextAccessor.HttpContext?
                .Session.GetString("JWToken");

        if (!string.IsNullOrWhiteSpace(token))
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    token);
        }

        return client;
    }

    // --------------------------------------------------
    // GET MY STUDENT PROFILE
    // GET: api/Students/me
    // --------------------------------------------------

    public async Task<StudentProfile?> GetMyProfileAsync()
    {
        var client = CreateClient();

        var response =
            await client.GetAsync("api/Students/me");

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content
            .ReadFromJsonAsync<StudentProfile>();
    }

    // --------------------------------------------------
    // CREATE / UPDATE MY PROFILE
    // PUT: api/Students/me
    // --------------------------------------------------

    public async Task<bool> SaveMyProfileAsync(
        StudentProfileRequest request)
    {
        var client = CreateClient();

        var response =
            await client.PutAsJsonAsync(
                "api/Students/me",
                request);

        return response.IsSuccessStatusCode;
    }

    // --------------------------------------------------
    // GET DEPARTMENTS
    // --------------------------------------------------

    public async Task<List<DepartmentItem>>
        GetDepartmentsAsync()
    {
        var client = CreateClient();

        var response =
            await client.GetAsync("api/Departments");

        if (!response.IsSuccessStatusCode)
        {
            return new List<DepartmentItem>();
        }

        return await response.Content
            .ReadFromJsonAsync<List<DepartmentItem>>()
            ?? new List<DepartmentItem>();
    }

    // --------------------------------------------------
    // GET MY RESUME
    // GET: api/Resumes/me
    // --------------------------------------------------

    public async Task<ResumeInfo?> GetResumeAsync(
        int studentId)
    {
        var client = CreateClient();

        var response =
            await client.GetAsync("api/Resumes/me");

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content
            .ReadFromJsonAsync<ResumeInfo>();
    }

    // --------------------------------------------------
    // UPLOAD / REPLACE RESUME
    // POST: api/Resumes/upload/{studentId}
    // --------------------------------------------------

    public async Task<ApiActionResult> UploadResumeAsync(
        int studentId,
        IFormFile file)
    {
        var client = CreateClient();

        using var content =
            new MultipartFormDataContent();

        await using var stream =
            file.OpenReadStream();

        using var fileContent =
            new StreamContent(stream);

        fileContent.Headers.ContentType =
            new MediaTypeHeaderValue(
                string.IsNullOrWhiteSpace(file.ContentType)
                    ? "application/pdf"
                    : file.ContentType);

        content.Add(
            fileContent,
            "file",
            file.FileName);

        var response =
            await client.PostAsync(
                $"api/Resumes/upload/{studentId}",
                content);

        if (response.IsSuccessStatusCode)
        {
            return new ApiActionResult
            {
                Success = true,
                Message =
                    "Resume uploaded successfully."
            };
        }

        var error =
            await response.Content
                .ReadAsStringAsync();

        return new ApiActionResult
        {
            Success = false,
            Message =
                ExtractApiMessage(
                    error,
                    "Resume upload failed.")
        };
    }

    // --------------------------------------------------
    // EXTRACT SKILLS
    // POST: api/AI/extract-skills/{studentId}
    // --------------------------------------------------

    public async Task<SkillExtractionResult?>
        ExtractSkillsAsync(int studentId)
    {
        var client = CreateClient();

        var response =
            await client.PostAsync(
                $"api/AI/extract-skills/{studentId}",
                null);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content
            .ReadFromJsonAsync<SkillExtractionResult>();
    }

    // --------------------------------------------------
    // AI JOB RECOMMENDATIONS
    // GET: api/AI/recommend-jobs/{studentId}
    // --------------------------------------------------

    public async Task<JobRecommendationResponse?>
        GetJobRecommendationsAsync(int studentId)
    {
        var client = CreateClient();

        var response =
            await client.GetAsync(
                $"api/AI/recommend-jobs/{studentId}");

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content
            .ReadFromJsonAsync<JobRecommendationResponse>();
    }

    // --------------------------------------------------
    // GET MY APPLICATIONS
    // GET: api/Applications/student/{studentId}
    // --------------------------------------------------

    public async Task<List<StudentApplicationItem>>
        GetMyApplicationsAsync(int studentId)
    {
        var client = CreateClient();

        var response =
            await client.GetAsync(
                $"api/Applications/student/{studentId}");

        if (!response.IsSuccessStatusCode)
        {
            return new List<StudentApplicationItem>();
        }

        return await response.Content
            .ReadFromJsonAsync<List<StudentApplicationItem>>()
            ?? new List<StudentApplicationItem>();
    }

    // --------------------------------------------------
    // APPLY FOR JOB
    // POST: api/Applications
    // --------------------------------------------------

    public async Task<ApiActionResult> ApplyForJobAsync(
        int studentId,
        int jobId)
    {
        var client = CreateClient();

        var request = new
        {
            StudentId = studentId,
            JobId = jobId
        };

        var response =
            await client.PostAsJsonAsync(
                "api/Applications",
                request);

        if (response.IsSuccessStatusCode)
        {
            return new ApiActionResult
            {
                Success = true,
                Message =
                    "Application submitted successfully."
            };
        }

        var error =
            await response.Content
                .ReadAsStringAsync();

        return new ApiActionResult
        {
            Success = false,
            Message =
                ExtractApiMessage(
                    error,
                    "Unable to submit application.")
        };
    }

    // --------------------------------------------------
    // READ API ERROR MESSAGE
    // --------------------------------------------------

    private static string ExtractApiMessage(
        string responseBody,
        string fallback)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return fallback;
        }

        try
        {
            using var document =
                JsonDocument.Parse(responseBody);

            if (document.RootElement.TryGetProperty(
                    "message",
                    out var message))
            {
                return message.GetString()
                    ?? fallback;
            }
        }
        catch
        {
            // Ignore malformed API error response.
        }

        return fallback;
    }
}


// ==================================================
// FRONTEND MODELS
// ==================================================

public class StudentProfile
{
    public bool ProfileExists { get; set; }

    public int StudentId { get; set; }

    public string FullName { get; set; } =
        string.Empty;

    public string Email { get; set; } =
        string.Empty;

    public decimal CGPA { get; set; }

    public int Backlogs { get; set; }

    public int GraduationYear { get; set; }

    public int DepartmentId { get; set; }

    public string? Department { get; set; }

    public List<string> Skills { get; set; } =
        new();
}

public class StudentProfileRequest
{
    public decimal CGPA { get; set; }

    public int Backlogs { get; set; }

    public int GraduationYear { get; set; }

    public int DepartmentId { get; set; }
}

public class DepartmentItem
{
    public int DepartmentId { get; set; }

    public string Name { get; set; } =
        string.Empty;
}

public class ResumeInfo
{
    public int ResumeId { get; set; }

    public string FileName { get; set; } =
        string.Empty;

    public string FilePath { get; set; } =
        string.Empty;

    public string ExtractedText { get; set; } =
        string.Empty;

    public DateTime UploadedAt { get; set; }

    public bool IsProcessed { get; set; }

    public int StudentId { get; set; }
}

public class SkillExtractionResult
{
    public int StudentId { get; set; }

    public List<string> DetectedSkills { get; set; } =
        new();

    public List<string> NewlyAddedSkills { get; set; } =
        new();

    public int TotalSkillsDetected { get; set; }

    public int TotalStudentSkills { get; set; }

    public List<string> StudentSkills { get; set; } =
        new();
}

public class ApiActionResult
{
    public bool Success { get; set; }

    public string Message { get; set; } =
        string.Empty;
}

// --------------------------------------------------
// JOB RECOMMENDATION RESPONSE
// --------------------------------------------------

public class JobRecommendationResponse
{
    public RecommendedStudent Student { get; set; } =
        new();

    public List<string> StudentSkills { get; set; } =
        new();

    public int TotalJobsAnalyzed { get; set; }

    public List<JobRecommendationItem> Recommendations
    { get; set; } = new();
}

public class RecommendedStudent
{
    public int StudentId { get; set; }

    public string FullName { get; set; } =
        string.Empty;

    public decimal CGPA { get; set; }

    public int Backlogs { get; set; }

    public int GraduationYear { get; set; }
}

public class JobRecommendationItem
{
    public int JobId { get; set; }

    public string JobTitle { get; set; } =
        string.Empty;

    public string? Company { get; set; }

    public decimal Package { get; set; }

    public string? Location { get; set; }

    public double MatchPercentage { get; set; }

    public bool AcademicallyEligible { get; set; }

    public List<string> MatchingSkills { get; set; } =
        new();

    public List<string> MissingSkills { get; set; } =
        new();

    public string Recommendation { get; set; } =
        string.Empty;
}

// --------------------------------------------------
// STUDENT APPLICATION
// --------------------------------------------------

public class StudentApplicationItem
{
    public int ApplicationId { get; set; }

    public int StudentId { get; set; }

    public int JobId { get; set; }

    public DateTime AppliedDate { get; set; }

    public string Status { get; set; } =
        string.Empty;

    public ApplicationJobInfo? Job { get; set; }
}

public class ApplicationJobInfo
{
    public int JobId { get; set; }

    public string Title { get; set; } =
        string.Empty;

    public string Description { get; set; } =
        string.Empty;

    public decimal Package { get; set; }

    public string? Location { get; set; }

    public string? EmploymentType { get; set; }

    public int CompanyId { get; set; }

    public ApplicationCompanyInfo? Company { get; set; }
}

public class ApplicationCompanyInfo
{
    public int CompanyId { get; set; }

    public string Name { get; set; } =
        string.Empty;
}