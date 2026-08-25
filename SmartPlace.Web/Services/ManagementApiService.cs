using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace SmartPlace.Web.Services;

public class ManagementApiService
{
    private readonly IHttpClientFactory
        _httpClientFactory;

    private readonly IHttpContextAccessor
        _httpContextAccessor;

    public ManagementApiService(
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor)
    {
        _httpClientFactory =
            httpClientFactory;

        _httpContextAccessor =
            httpContextAccessor;
    }

    // ==================================================
    // AUTHENTICATED CLIENT
    // ==================================================

    private HttpClient CreateClient()
    {
        var client =
            _httpClientFactory.CreateClient(
                "SmartPlaceAPI");

        var token =
            _httpContextAccessor
                .HttpContext?
                .Session
                .GetString("JWToken");

        if (!string.IsNullOrWhiteSpace(
            token))
        {
            client.DefaultRequestHeaders
                .Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    token);
        }

        return client;
    }

    // ==================================================
    // COMPANIES
    // ==================================================

    public async Task<List<ManagementCompany>>
        GetCompaniesAsync()
    {
        var client = CreateClient();

        var response =
            await client.GetAsync(
                "api/Companies");

        if (!response.IsSuccessStatusCode)
        {
            return new();
        }

        return await response.Content
            .ReadFromJsonAsync<
                List<ManagementCompany>>()
            ?? new();
    }

    public async Task<ApiActionResult>
        CreateCompanyAsync(
            ManagementCompanyRequest request)
    {
        var client = CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "api/Companies",
                request);

        return await ToResultAsync(
            response,
            "Company submitted for approval successfully.",
            "Unable to register company.");
    }

    public async Task<ApiActionResult>
        UpdateCompanyApprovalAsync(
            int companyId,
            string status)
    {
        var client = CreateClient();

        using var content =
            JsonContent.Create(status);

        var response =
            await client.PutAsync(
                $"api/Companies/{companyId}/approval",
                content);

        return await ToResultAsync(
            response,
            $"Company status changed to {status}.",
            "Unable to update company approval.");
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

    public async Task<ApiActionResult>
        CreateDepartmentAsync(
            string name)
    {
        var client = CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "api/Departments",
                new
                {
                    Name = name
                });

        return await ToResultAsync(
            response,
            "Department created successfully.",
            "Unable to create department.");
    }

    // ==================================================
    // SKILLS
    // ==================================================

    public async Task<List<ManagementSkill>>
        GetSkillsAsync()
    {
        var client = CreateClient();

        var response =
            await client.GetAsync(
                "api/Skills");

        if (!response.IsSuccessStatusCode)
        {
            return new();
        }

        return await response.Content
            .ReadFromJsonAsync<
                List<ManagementSkill>>()
            ?? new();
    }

    public async Task<ApiActionResult>
        CreateSkillAsync(
            string name)
    {
        var client = CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "api/Skills",
                new
                {
                    Name = name
                });

        return await ToResultAsync(
            response,
            "Skill created successfully.",
            "Unable to create skill.");
    }

    // ==================================================
    // JOBS
    // ==================================================

    public async Task<List<ManagementJob>>
        GetJobsAsync()
    {
        var client = CreateClient();

        var response =
            await client.GetAsync(
                "api/Jobs");

        if (!response.IsSuccessStatusCode)
        {
            return new();
        }

        return await response.Content
            .ReadFromJsonAsync<
                List<ManagementJob>>()
            ?? new();
    }

    public async Task<ApiActionResult>
        CreateJobAsync(
            ManagementJobRequest request)
    {
        var client = CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "api/Jobs",
                request);

        return await ToResultAsync(
            response,
            "Job created successfully and sent for approval.",
            "Unable to create job.");
    }

    public async Task<ApiActionResult>
        UpdateJobStatusAsync(
            int jobId,
            string status)
    {
        var client = CreateClient();

        using var content =
            JsonContent.Create(status);

        var response =
            await client.PutAsync(
                $"api/Jobs/{jobId}/status",
                content);

        return await ToResultAsync(
            response,
            $"Job status changed to {status}.",
            "Unable to update job status.");
    }

    // ==================================================
    // STUDENTS
    // ==================================================

    public async Task<List<ManagementStudent>>
        GetStudentsAsync()
    {
        var client = CreateClient();

        var response =
            await client.GetAsync(
                "api/Students");

        if (!response.IsSuccessStatusCode)
        {
            return new();
        }

        return await response.Content
            .ReadFromJsonAsync<
                List<ManagementStudent>>()
            ?? new();
    }

    // ==================================================
    // APPLICATIONS
    // ==================================================

    public async Task<
        List<ManagementApplication>>
        GetApplicationsAsync()
    {
        var client = CreateClient();

        var response =
            await client.GetAsync(
                "api/Applications");

        if (!response.IsSuccessStatusCode)
        {
            return new();
        }

        return await response.Content
            .ReadFromJsonAsync<
                List<ManagementApplication>>()
            ?? new();
    }

    public async Task<ApiActionResult>
        UpdateApplicationStatusAsync(
            int applicationId,
            string status)
    {
        var client = CreateClient();

        using var content =
            JsonContent.Create(status);

        var response =
            await client.PutAsync(
                $"api/Applications/{applicationId}/status",
                content);

        return await ToResultAsync(
            response,
            $"Application changed to {status}.",
            "Unable to update application.");
    }

    // ==================================================
    // INTERVIEWS
    // ==================================================

    public async Task<List<ManagementInterview>>
        GetInterviewsAsync()
    {
        var client = CreateClient();

        var response =
            await client.GetAsync(
                "api/InterviewRounds");

        if (!response.IsSuccessStatusCode)
        {
            return new();
        }

        return await response.Content
            .ReadFromJsonAsync<
                List<ManagementInterview>>()
            ?? new();
    }

    public async Task<ApiActionResult>
        CreateInterviewAsync(
            ManagementInterviewRequest request)
    {
        var client = CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "api/InterviewRounds",
                request);

        return await ToResultAsync(
            response,
            "Interview scheduled successfully.",
            "Unable to schedule interview.");
    }

    public async Task<ApiActionResult>
        UpdateInterviewResultAsync(
            int interviewId,
            string result)
    {
        var client = CreateClient();

        using var content =
            JsonContent.Create(result);

        var response =
            await client.PutAsync(
                $"api/InterviewRounds/{interviewId}/result",
                content);

        return await ToResultAsync(
            response,
            $"Interview result changed to {result}.",
            "Unable to update interview result.");
    }

    // ==================================================
    // PLACEMENTS
    // ==================================================

    public async Task<List<ManagementPlacement>>
        GetPlacementsAsync()
    {
        var client = CreateClient();

        var response =
            await client.GetAsync(
                "api/Placements");

        if (!response.IsSuccessStatusCode)
        {
            return new();
        }

        return await response.Content
            .ReadFromJsonAsync<
                List<ManagementPlacement>>()
            ?? new();
    }

    public async Task<ApiActionResult>
        CreatePlacementAsync(
            ManagementPlacementRequest request)
    {
        var client = CreateClient();

        var response =
            await client.PostAsJsonAsync(
                "api/Placements",
                request);

        return await ToResultAsync(
            response,
            "Placement recorded successfully.",
            "Unable to create placement.");
    }

    // ==================================================
    // RESPONSE HANDLING
    // ==================================================

    private static async Task<ApiActionResult>
        ToResultAsync(
            HttpResponseMessage response,
            string successMessage,
            string fallbackError)
    {
        if (response.IsSuccessStatusCode)
        {
            return new ApiActionResult
            {
                Success = true,
                Message =
                    successMessage
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
                    fallbackError)
        };
    }

    private static string ExtractApiMessage(
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
// COMPANY
// ==================================================

public class ManagementCompany
{
    public int CompanyId { get; set; }

    public string Name { get; set; } =
        string.Empty;

    public string Industry { get; set; } =
        string.Empty;

    public string Location { get; set; } =
        string.Empty;

    public string Website { get; set; } =
        string.Empty;

    public string Description { get; set; } =
        string.Empty;

    public string ApprovalStatus { get; set; } =
        string.Empty;

    public string? RecruiterUserId
    { get; set; }
}

public class ManagementCompanyRequest
{
    public string Name { get; set; } =
        string.Empty;

    public string Industry { get; set; } =
        string.Empty;

    public string Location { get; set; } =
        string.Empty;

    public string Website { get; set; } =
        string.Empty;

    public string Description { get; set; } =
        string.Empty;
}


// ==================================================
// JOB
// ==================================================

public class ManagementJob
{
    public int JobId { get; set; }

    public string Title { get; set; } =
        string.Empty;

    public string Description { get; set; } =
        string.Empty;

    public decimal Package { get; set; }

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

    public int? RequiredDepartmentId
    { get; set; }

    public DepartmentItem? RequiredDepartment
    { get; set; }

    public string Location { get; set; } =
        string.Empty;

    public string EmploymentType { get; set; } =
        string.Empty;

    public DateTime PostedDate { get; set; }

    public DateTime? ApplicationDeadline
    { get; set; }

    public string Status { get; set; } =
        string.Empty;

    public int CompanyId { get; set; }

    public ManagementCompany? Company
    { get; set; }
}

public class ManagementJobRequest
{
    public string Title { get; set; } =
        string.Empty;

    public string Description { get; set; } =
        string.Empty;

    public decimal Package { get; set; }

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

    public int RequiredDepartmentId
    { get; set; }

    public string Location { get; set; } =
        string.Empty;

    public string EmploymentType { get; set; } =
        "Full-Time";

    public DateTime? ApplicationDeadline
    { get; set; }

    public int CompanyId { get; set; }
}


// ==================================================
// STUDENT
// ==================================================

public class ManagementStudent
{
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

    public DepartmentItem? Department
    { get; set; }
}


// ==================================================
// APPLICATION
// ==================================================

public class ManagementApplication
{
    public int ApplicationId { get; set; }

    public DateTime AppliedDate { get; set; }

    public string Status { get; set; } =
        string.Empty;

    public string? Remarks { get; set; }

    public int StudentId { get; set; }

    public int JobId { get; set; }

    public ManagementStudent? Student
    { get; set; }

    public ManagementJob? Job
    { get; set; }
}


// ==================================================
// INTERVIEW
// ==================================================

public class ManagementInterview
{
    public int InterviewRoundId { get; set; }

    public string RoundName { get; set; } =
        string.Empty;

    public DateTime ScheduledDate
    { get; set; }

    public string Mode { get; set; } =
        string.Empty;

    public string? LocationOrLink
    { get; set; }

    public string Status { get; set; } =
        string.Empty;

    public string? Result { get; set; }

    public string? Remarks { get; set; }

    public int ApplicationId { get; set; }

    public ManagementApplication? Application
    { get; set; }
}

public class ManagementInterviewRequest
{
    public string RoundName { get; set; } =
        string.Empty;

    public DateTime ScheduledDate
    { get; set; }

    public string Mode { get; set; } =
        "Online";

    public string? LocationOrLink
    { get; set; }

    public string? Remarks { get; set; }

    public int ApplicationId { get; set; }
}


// ==================================================
// PLACEMENT
// ==================================================

public class ManagementPlacement
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

    public ManagementStudent? Student
    { get; set; }

    public ManagementCompany? Company
    { get; set; }
}

public class ManagementPlacementRequest
{
    public decimal OfferedPackage
    { get; set; }

    public DateTime? JoiningDate
    { get; set; }

    public string? OfferLetterUrl
    { get; set; }

    public int StudentId { get; set; }

    public int CompanyId { get; set; }
}


// ==================================================
// SKILL
// ==================================================

public class ManagementSkill
{
    public int SkillId { get; set; }

    public string Name { get; set; } =
        string.Empty;
}