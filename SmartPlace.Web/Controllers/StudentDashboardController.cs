using Microsoft.AspNetCore.Mvc;
using SmartPlace.Web.Services;

namespace SmartPlace.Web.Controllers;

public class StudentDashboardController : Controller
{
    private readonly StudentApiService
        _studentApiService;

    public StudentDashboardController(
        StudentApiService studentApiService)
    {
        _studentApiService =
            studentApiService;
    }

    // ==================================================
    // DASHBOARD
    // ==================================================

    public async Task<IActionResult> Index()
    {
        if (!IsStudentLoggedIn())
        {
            return LoginRedirect();
        }

        var profile =
            await _studentApiService
                .GetMyProfileAsync();

        if (profile == null)
        {
            return RedirectToAction(
                nameof(CompleteProfile));
        }

        ViewBag.AcademicProfileIncomplete =
            profile.TenthPercentage <= 0
            ||
            profile.TwelfthPercentage <= 0;

        return View(profile);
    }

    // ==================================================
    // PROFILE GET
    // ==================================================

    [HttpGet]
    public async Task<IActionResult>
        CompleteProfile()
    {
        if (!IsStudentLoggedIn())
        {
            return LoginRedirect();
        }

        ViewBag.Departments =
            await _studentApiService
                .GetDepartmentsAsync();

        var profile =
            await _studentApiService
                .GetMyProfileAsync();

        if (profile == null)
        {
            return View(
                new StudentProfileRequest
                {
                    GraduationYear =
                        DateTime.Now.Year
                });
        }

        return View(
            new StudentProfileRequest
            {
                TenthPercentage =
                    profile.TenthPercentage,

                TwelfthPercentage =
                    profile.TwelfthPercentage,

                CGPA =
                    profile.CGPA,

                Backlogs =
                    profile.Backlogs,

                GraduationYear =
                    profile.GraduationYear,

                DepartmentId =
                    profile.DepartmentId
            });
    }

    // ==================================================
    // PROFILE POST
    // ==================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult>
        CompleteProfile(
            StudentProfileRequest model)
    {
        if (!IsStudentLoggedIn())
        {
            return LoginRedirect();
        }

        var currentYear =
            DateTime.Now.Year;

        if (model.TenthPercentage <= 0 ||
            model.TenthPercentage > 100)
        {
            ModelState.AddModelError(
                nameof(
                    model.TenthPercentage),
                "Enter a valid 10th percentage between 0 and 100.");
        }

        if (model.TwelfthPercentage <= 0 ||
            model.TwelfthPercentage > 100)
        {
            ModelState.AddModelError(
                nameof(
                    model.TwelfthPercentage),
                "Enter a valid 12th percentage between 0 and 100.");
        }

        if (model.CGPA < 0 ||
            model.CGPA > 10)
        {
            ModelState.AddModelError(
                nameof(model.CGPA),
                "CGPA must be between 0 and 10.");
        }

        if (model.Backlogs < 0)
        {
            ModelState.AddModelError(
                nameof(model.Backlogs),
                "Backlogs cannot be negative.");
        }

        if (model.GraduationYear <
                currentYear ||
            model.GraduationYear >
                currentYear + 10)
        {
            ModelState.AddModelError(
                nameof(
                    model.GraduationYear),
                $"Graduation year must be between {currentYear} and {currentYear + 10}.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Departments =
                await _studentApiService
                    .GetDepartmentsAsync();

            return View(model);
        }

        var result =
            await _studentApiService
                .SaveMyProfileAsync(model);

        if (!result.Success)
        {
            ViewBag.Error =
                result.Message;

            ViewBag.Departments =
                await _studentApiService
                    .GetDepartmentsAsync();

            return View(model);
        }

        TempData["Success"] =
            "Academic profile saved successfully.";

        return RedirectToAction(
            nameof(Index));
    }

    // ==================================================
    // JOBS
    // ==================================================

    [HttpGet]
    public async Task<IActionResult> Jobs()
    {
        var profile =
            await RequireProfileAsync();

        if (profile == null)
        {
            return LoginOrProfileRedirect();
        }

        if (profile.TenthPercentage <= 0 ||
            profile.TwelfthPercentage <= 0)
        {
            TempData["Error"] =
                "Complete your 10th and 12th academic details before checking job eligibility.";

            return RedirectToAction(
                nameof(CompleteProfile));
        }

        var jobs =
            await _studentApiService
                .GetStudentJobsAsync(
                    profile.StudentId);

        if (jobs == null)
        {
            jobs =
                new StudentJobsResponse();

            ViewBag.Error =
                "Unable to load jobs right now.";
        }

        JobRecommendationResponse? recommendations =
            null;

        if (profile.Skills.Count > 0)
        {
            recommendations =
                await _studentApiService
                    .GetJobRecommendationsAsync(
                        profile.StudentId);
        }

        var model =
            new StudentJobsPageViewModel
            {
                Profile = profile,

                Jobs = jobs,

                Recommendations =
                    recommendations
                    ?? new JobRecommendationResponse()
            };

        return View(model);
    }

    // ==================================================
    // RESUME
    // ==================================================

    [HttpGet]
    public async Task<IActionResult> Resume()
    {
        var profile =
            await RequireProfileAsync();

        if (profile == null)
        {
            return LoginOrProfileRedirect();
        }

        var resume =
            await _studentApiService
                .GetResumeAsync(
                    profile.StudentId);

        return View(
            new ResumePageViewModel
            {
                StudentId =
                    profile.StudentId,

                Resume =
                    resume,

                Skills =
                    profile.Skills
            });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult>
        UploadResume(IFormFile file)
    {
        var profile =
            await RequireProfileAsync();

        if (profile == null)
        {
            return LoginOrProfileRedirect();
        }

        if (file == null ||
            file.Length == 0)
        {
            TempData["Error"] =
                "Please select a PDF resume.";

            return RedirectToAction(
                nameof(Resume));
        }

        var result =
            await _studentApiService
                .UploadResumeAsync(
                    profile.StudentId,
                    file);

        TempData[
            result.Success
                ? "Success"
                : "Error"] =
            result.Success
                ? "Resume uploaded and text extracted successfully."
                : result.Message;

        return RedirectToAction(
            nameof(Resume));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult>
        ExtractSkills()
    {
        var profile =
            await RequireProfileAsync();

        if (profile == null)
        {
            return LoginOrProfileRedirect();
        }

        var result =
            await _studentApiService
                .ExtractSkillsAsync(
                    profile.StudentId);

        if (result == null)
        {
            TempData["Error"] =
                "Unable to extract skills.";
        }
        else
        {
            TempData["Success"] =
                $"Skill extraction completed. {result.TotalSkillsDetected} skills detected.";
        }

        return RedirectToAction(
            nameof(Resume));
    }

    // ==================================================
    // EXISTING RECOMMENDATIONS PAGE
    // ==================================================

    [HttpGet]
    public async Task<IActionResult>
        Recommendations()
    {
        var profile =
            await RequireProfileAsync();

        if (profile == null)
        {
            return LoginOrProfileRedirect();
        }

        if (profile.TenthPercentage <= 0 ||
            profile.TwelfthPercentage <= 0)
        {
            TempData["Error"] =
                "Complete your academic profile first.";

            return RedirectToAction(
                nameof(CompleteProfile));
        }

        if (profile.Skills.Count == 0)
        {
            TempData["Error"] =
                "Upload your resume and extract skills first.";

            return RedirectToAction(
                nameof(Resume));
        }

        var result =
            await _studentApiService
                .GetJobRecommendationsAsync(
                    profile.StudentId);

        if (result == null)
        {
            ViewBag.Error =
                "No AI recommendations are currently available.";

            return View(
                new JobRecommendationResponse());
        }

        return View(result);
    }

    // ==================================================
    // APPLY
    // ==================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult>
        Apply(int jobId)
    {
        var profile =
            await RequireProfileAsync();

        if (profile == null)
        {
            return LoginOrProfileRedirect();
        }

        var result =
            await _studentApiService
                .ApplyForJobAsync(
                    profile.StudentId,
                    jobId);

        TempData[
            result.Success
                ? "Success"
                : "Error"] =
            result.Message;

        if (result.Success)
        {
            return RedirectToAction(
                nameof(MyApplications));
        }

        return RedirectToAction(
            nameof(Jobs));
    }

    // ==================================================
    // APPLICATIONS
    // ==================================================

    [HttpGet]
    public async Task<IActionResult>
        MyApplications()
    {
        var profile =
            await RequireProfileAsync();

        if (profile == null)
        {
            return LoginOrProfileRedirect();
        }

        var applications =
            await _studentApiService
                .GetMyApplicationsAsync(
                    profile.StudentId);

        return View(applications);
    }

    // ==================================================
    // INTERVIEWS
    // ==================================================

    [HttpGet]
    public async Task<IActionResult>
        Interviews()
    {
        var profile =
            await RequireProfileAsync();

        if (profile == null)
        {
            return LoginOrProfileRedirect();
        }

        var interviews =
            await _studentApiService
                .GetMyInterviewsAsync(
                    profile.StudentId);

        return View(interviews);
    }

    // ==================================================
    // PLACEMENT
    // ==================================================

    [HttpGet]
    public async Task<IActionResult>
        Placement()
    {
        var profile =
            await RequireProfileAsync();

        if (profile == null)
        {
            return LoginOrProfileRedirect();
        }

        var placement =
            await _studentApiService
                .GetMyPlacementAsync(
                    profile.StudentId);

        return View(placement);
    }

    // ==================================================
    // HELPERS
    // ==================================================

    private bool IsStudentLoggedIn()
    {
        var token =
            HttpContext.Session
                .GetString("JWToken");

        var role =
            HttpContext.Session
                .GetString("UserRole");

        return
            !string.IsNullOrWhiteSpace(token)
            &&
            string.Equals(
                role,
                "Student",
                StringComparison.OrdinalIgnoreCase);
    }

    private async Task<StudentProfile?>
        RequireProfileAsync()
    {
        if (!IsStudentLoggedIn())
        {
            return null;
        }

        return await _studentApiService
            .GetMyProfileAsync();
    }

    private IActionResult LoginRedirect()
    {
        return RedirectToAction(
            "Login",
            "Account");
    }

    private IActionResult
        LoginOrProfileRedirect()
    {
        if (!IsStudentLoggedIn())
        {
            return LoginRedirect();
        }

        return RedirectToAction(
            nameof(CompleteProfile));
    }
}


// ==================================================
// VIEW MODELS
// ==================================================

public class ResumePageViewModel
{
    public int StudentId { get; set; }

    public ResumeInfo? Resume { get; set; }

    public List<string> Skills
    { get; set; } = new();
}

public class StudentJobsPageViewModel
{
    public StudentProfile Profile
    { get; set; } = new();

    public StudentJobsResponse Jobs
    { get; set; } = new();

    public JobRecommendationResponse Recommendations
    { get; set; } = new();
}