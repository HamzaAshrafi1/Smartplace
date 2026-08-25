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

    // ==================================================
    // AUTHENTICATED API CLIENT
    // ==================================================

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

    // ==================================================
    // PROFILE
    // ==================================================

    public async Task<StudentProfile?>
        GetMyProfileAsync()
    {
        var client = CreateClient();

        var response =
            await client.GetAsync(
                "api/Students/me");

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content
            .ReadFromJsonAsync<StudentProfile>();
    }

    public async Task<ApiActionResult>
        SaveMyProfileAsync(
            StudentProfileRequest request)
    {
        var client = CreateClient();

        var response =
            await client.PutAsJsonAsync(
                "api/Students/me",
                request);

        if (response.IsSuccessStatusCode)
        {
            return new ApiActionResult
            {
                Success = true,
                Message =
                    "Profile saved successfully."
            };
        }

        var body =
            await response.Content
                .ReadAsStringAsync();

        return new ApiActionResult
        {
            Success = false,
            Message =
                ExtractApiMessage(
                    body,
                    "Unable to save profile.")
        };
    }

    // ==================================================
    // DEPARTMENTS
    // ==================================================

    public async Task<List<DepartmentItem>>
        GetDepartmentsAsync()
    {
        var client = CreateClient();

        var response =
            await client.GetAsync(
                "api/Departments");

        if (!response.IsSuccessStatusCode)
        {
            return new();
        }

        return await response.Content
            .ReadFromJsonAsync<
                List<DepartmentItem>>()
            ?? new();
    }

    // ==================================================
    // STUDENT JOB ELIGIBILITY
    // GET api/Jobs/student/{id}/eligibility
    // ==================================================

    public async Task<StudentJobsResponse?>
        GetStudentJobsAsync(
            int studentId)
    {
        var client = CreateClient();

        var response =
            await client.GetAsync(
                $"api/Jobs/student/{studentId}/eligibility");

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content
            .ReadFromJsonAsync<StudentJobsResponse>();
    }

    // ==================================================
    // RESUME
    // ==================================================

    public async Task<ResumeInfo?>
        GetResumeAsync(int studentId)
    {
        var client = CreateClient();

        var response =
            await client.GetAsync(
                "api/Resumes/me");

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content
            .ReadFromJsonAsync<ResumeInfo>();
    }

    public async Task<ApiActionResult>
        UploadResumeAsync(
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
                string.IsNullOrWhiteSpace(
                    file.ContentType)
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

        return new ApiActionResult
        {
            Success = false,
            Message =
                ExtractApiMessage(
                    await response.Content
                        .ReadAsStringAsync(),
                    "Resume upload failed.")
        };
    }

    // ==================================================
    // SKILLS
    // ==================================================

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
            .ReadFromJsonAsync<
                SkillExtractionResult>();
    }

    // ==================================================
    // AI RECOMMENDATIONS
    // ==================================================

    public async Task<JobRecommendationResponse?>
        GetJobRecommendationsAsync(
            int studentId)
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
            .ReadFromJsonAsync<
                JobRecommendationResponse>();
    }

    // ==================================================
    // APPLICATIONS
    // ==================================================

    public async Task<List<StudentApplicationItem>>
        GetMyApplicationsAsync(
            int studentId)
    {
        var client = CreateClient();

        var response =
            await client.GetAsync(
                $"api/Applications/student/{studentId}");

        if (!response.IsSuccessStatusCode)
        {
            return new();
        }

        return await response.Content
            .ReadFromJsonAsync<
                List<StudentApplicationItem>>()
            ?? new();
    }

    public async Task<ApiActionResult>
        ApplyForJobAsync(
            int studentId,
            int jobId)
    {
        var client = CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "api/Applications",
                new
                {
                    StudentId = studentId,
                    JobId = jobId
                });

        if (response.IsSuccessStatusCode)
        {
            return new ApiActionResult
            {
                Success = true,
                Message =
                    "Application submitted successfully."
            };
        }

        var body =
            await response.Content
                .ReadAsStringAsync();

        return new ApiActionResult
        {
            Success = false,
            Message =
                ExtractApiMessage(
                    body,
                    "Unable to submit application.")
        };
    }

    // ==================================================
    // INTERVIEWS
    // ==================================================

    public async Task<List<StudentInterviewItem>>
        GetMyInterviewsAsync(
            int studentId)
    {
        var applications =
            await GetMyApplicationsAsync(
                studentId);

        var result =
            new List<StudentInterviewItem>();

        var client = CreateClient();

        foreach (var application
                 in applications)
        {
            var response =
                await client.GetAsync(
                    $"api/InterviewRounds/application/{application.ApplicationId}");

            if (!response.IsSuccessStatusCode)
            {
                continue;
            }

            var interviews =
                await response.Content
                    .ReadFromJsonAsync<
                        List<StudentInterviewItem>>();

            if (interviews == null)
            {
                continue;
            }

            foreach (var interview
                     in interviews)
            {
                interview.JobTitle =
                    application.Job?.Title
                    ?? "Job";

                interview.CompanyName =
                    application.Job?
                        .Company?.Name
                    ?? "Company";
            }

            result.AddRange(
                interviews);
        }

        return result
            .OrderBy(i =>
                i.ScheduledDate)
            .ToList();
    }

    // ==================================================
    // PLACEMENT
    // ==================================================

    public async Task<StudentPlacementInfo?>
        GetMyPlacementAsync(
            int studentId)
    {
        var client = CreateClient();

        var response =
            await client.GetAsync(
                $"api/Placements/student/{studentId}");

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content
            .ReadFromJsonAsync<
                StudentPlacementInfo>();
    }

    // ==================================================
    // ERROR PARSER
    // ==================================================

    private static string
        ExtractApiMessage(
            string body,
            string fallback)
    {
        if (string.IsNullOrWhiteSpace(
            body))
        {
            return fallback;
        }

        try
        {
            using var document =
                JsonDocument.Parse(body);

            if (document.RootElement
                .TryGetProperty(
                    "message",
                    out var message))
            {
                return message.GetString()
                    ?? fallback;
            }
        }
        catch
        {
        }

        return fallback;
    }
}


// ==================================================
// PROFILE
// ==================================================

public class StudentProfile
{
    public bool ProfileExists { get; set; }

    public int StudentId { get; set; }

    public string FullName { get; set; } =
        string.Empty;

    public string Email { get; set; } =
        string.Empty;

    public decimal TenthPercentage
    { get; set; }

    public decimal TwelfthPercentage
    { get; set; }

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
    public decimal TenthPercentage
    { get; set; }

    public decimal TwelfthPercentage
    { get; set; }

    public decimal CGPA { get; set; }

    public int Backlogs { get; set; }

    public int GraduationYear { get; set; }

    public int DepartmentId { get; set; }
}


// ==================================================
// DEPARTMENT
// ==================================================

public class DepartmentItem
{
    public int DepartmentId { get; set; }

    public string Name { get; set; } =
        string.Empty;
}


// ==================================================
// JOB ELIGIBILITY
// ==================================================

public class StudentJobsResponse
{
    public int TotalJobs { get; set; }

    public int EligibleCount { get; set; }

    public int NotEligibleCount { get; set; }

    public List<StudentJobItem> Jobs
    { get; set; } = new();
}

public class StudentJobItem
{
    public int JobId { get; set; }

    public string Title { get; set; } =
        string.Empty;

    public string? Company { get; set; }

    public decimal Package { get; set; }

    public string? Location { get; set; }

    public string? EmploymentType
    { get; set; }

    public DateTime? ApplicationDeadline
    { get; set; }

    public bool Eligible { get; set; }

    public JobRequirementInfo Requirements
    { get; set; } = new();

    public StudentAcademicValues StudentValues
    { get; set; } = new();

    public List<string> Reasons
    { get; set; } = new();
}

public class JobRequirementInfo
{
    public string? Department { get; set; }

    public decimal MinimumTenthPercentage
    { get; set; }

    public decimal MinimumTwelfthPercentage
    { get; set; }

    public decimal MinimumCGPA
    { get; set; }

    public int MaximumBacklogs
    { get; set; }

    public int GraduationYear
    { get; set; }
}

public class StudentAcademicValues
{
    public string? Department { get; set; }

    public decimal TenthPercentage
    { get; set; }

    public decimal TwelfthPercentage
    { get; set; }

    public decimal CGPA { get; set; }

    public int Backlogs { get; set; }

    public int GraduationYear { get; set; }
}


// ==================================================
// RESUME
// ==================================================

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


// ==================================================
// SKILLS
// ==================================================

public class SkillExtractionResult
{
    public int StudentId { get; set; }

    public List<string> DetectedSkills
    { get; set; } = new();

    public List<string> NewlyAddedSkills
    { get; set; } = new();

    public int TotalSkillsDetected
    { get; set; }

    public int TotalStudentSkills
    { get; set; }

    public List<string> StudentSkills
    { get; set; } = new();
}


// ==================================================
// GENERIC API RESULT
// ==================================================

public class ApiActionResult
{
    public bool Success { get; set; }

    public string Message { get; set; } =
        string.Empty;
}


// ==================================================
// AI RECOMMENDATIONS
// ==================================================

public class JobRecommendationResponse
{
    public RecommendedStudent Student
    { get; set; } = new();

    public List<string> StudentSkills
    { get; set; } = new();

    public int TotalJobsAnalyzed
    { get; set; }

    public int TotalPublishedJobs
    { get; set; }

    public int TotalEligibleJobs
    { get; set; }

    public List<JobRecommendationItem>
        Recommendations
    { get; set; } = new();
}

public class RecommendedStudent
{
    public int StudentId { get; set; }

    public string FullName { get; set; } =
        string.Empty;

    public decimal TenthPercentage
    { get; set; }

    public decimal TwelfthPercentage
    { get; set; }

    public decimal CGPA { get; set; }

    public int Backlogs { get; set; }

    public int GraduationYear { get; set; }

    public string? Department { get; set; }
}

public class JobRecommendationItem
{
    public int JobId { get; set; }

    public string JobTitle { get; set; } =
        string.Empty;

    public string? Company { get; set; }

    public decimal Package { get; set; }

    public string? Location { get; set; }

    public double MatchPercentage
    { get; set; }

    public bool AcademicallyEligible
    { get; set; }

    public List<string> MatchingSkills
    { get; set; } = new();

    public List<string> MissingSkills
    { get; set; } = new();

    public string Recommendation
    { get; set; } =
        string.Empty;
}


// ==================================================
// APPLICATION
// ==================================================

public class StudentApplicationItem
{
    public int ApplicationId { get; set; }

    public int StudentId { get; set; }

    public int JobId { get; set; }

    public DateTime AppliedDate { get; set; }

    public string Status { get; set; } =
        string.Empty;

    public string? Remarks { get; set; }

    public ApplicationJobInfo? Job
    { get; set; }
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

    public string? EmploymentType
    { get; set; }

    public int CompanyId { get; set; }

    public ApplicationCompanyInfo? Company
    { get; set; }
}

public class ApplicationCompanyInfo
{
    public int CompanyId { get; set; }

    public string Name { get; set; } =
        string.Empty;
}


// ==================================================
// INTERVIEW
// ==================================================

public class StudentInterviewItem
{
    public int InterviewRoundId { get; set; }

    public string RoundName { get; set; } =
        string.Empty;

    public DateTime ScheduledDate { get; set; }

    public string Mode { get; set; } =
        string.Empty;

    public string? LocationOrLink
    { get; set; }

    public string Status { get; set; } =
        string.Empty;

    public string? Result { get; set; }

    public string? Remarks { get; set; }

    public int ApplicationId { get; set; }

    public string JobTitle { get; set; } =
        string.Empty;

    public string CompanyName { get; set; } =
        string.Empty;
}


// ==================================================
// PLACEMENT
// ==================================================

public class StudentPlacementInfo
{
    public int PlacementId { get; set; }

    public decimal OfferedPackage
    { get; set; }

    public DateTime? JoiningDate
    { get; set; }

    public string Status { get; set; } =
        string.Empty;

    public string? OfferLetterUrl
    { get; set; }

    public int StudentId { get; set; }

    public int CompanyId { get; set; }

    public ApplicationCompanyInfo? Company
    { get; set; }
}