using Microsoft.AspNetCore.Mvc;
using SmartPlace.Web.Services;

namespace SmartPlace.Web.Controllers;

public class StudentDashboardController : Controller
{
    private readonly StudentApiService _studentApiService;

    public StudentDashboardController(
        StudentApiService studentApiService)
    {
        _studentApiService = studentApiService;
    }

    // --------------------------------------------------
    // STUDENT DASHBOARD
    // --------------------------------------------------

    public async Task<IActionResult> Index()
    {
        if (!IsStudentLoggedIn())
        {
            return RedirectToAction(
                "Login",
                "Account");
        }

        var profile =
            await _studentApiService.GetMyProfileAsync();

        if (profile == null)
        {
            return RedirectToAction(
                nameof(CompleteProfile));
        }

        ViewBag.UserName =
            HttpContext.Session.GetString("UserName");

        ViewBag.UserEmail =
            HttpContext.Session.GetString("UserEmail");

        return View(profile);
    }

    // --------------------------------------------------
    // COMPLETE PROFILE
    // GET
    // --------------------------------------------------

    [HttpGet]
    public async Task<IActionResult> CompleteProfile()
    {
        if (!IsStudentLoggedIn())
        {
            return RedirectToAction(
                "Login",
                "Account");
        }

        ViewBag.Departments =
            await _studentApiService
                .GetDepartmentsAsync();

        var existingProfile =
            await _studentApiService
                .GetMyProfileAsync();

        if (existingProfile != null)
        {
            var model = new StudentProfileRequest
            {
                CGPA = existingProfile.CGPA,
                Backlogs = existingProfile.Backlogs,
                GraduationYear =
                    existingProfile.GraduationYear,
                DepartmentId =
                    existingProfile.DepartmentId
            };

            return View(model);
        }

        return View(
            new StudentProfileRequest());
    }

    // --------------------------------------------------
    // COMPLETE PROFILE
    // POST
    // --------------------------------------------------

    [HttpPost]
    public async Task<IActionResult> CompleteProfile(
        StudentProfileRequest model)
    {
        if (!IsStudentLoggedIn())
        {
            return RedirectToAction(
                "Login",
                "Account");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Departments =
                await _studentApiService
                    .GetDepartmentsAsync();

            return View(model);
        }

        var success =
            await _studentApiService
                .SaveMyProfileAsync(model);

        if (!success)
        {
            ViewBag.Error =
                "Unable to save profile. Please check the entered details.";

            ViewBag.Departments =
                await _studentApiService
                    .GetDepartmentsAsync();

            return View(model);
        }

        TempData["Success"] =
            "Profile saved successfully.";

        return RedirectToAction(
            nameof(Index));
    }

    // --------------------------------------------------
    // RESUME PAGE
    // --------------------------------------------------

    [HttpGet]
    public async Task<IActionResult> Resume()
    {
        if (!IsStudentLoggedIn())
        {
            return RedirectToAction(
                "Login",
                "Account");
        }

        var profile =
            await _studentApiService
                .GetMyProfileAsync();

        if (profile == null)
        {
            return RedirectToAction(
                nameof(CompleteProfile));
        }

        var resume =
            await _studentApiService
                .GetResumeAsync(
                    profile.StudentId);

        var model = new ResumePageViewModel
        {
            StudentId =
                profile.StudentId,

            Resume =
                resume,

            Skills =
                profile.Skills
        };

        return View(model);
    }

    // --------------------------------------------------
    // UPLOAD RESUME
    // --------------------------------------------------

    [HttpPost]
    public async Task<IActionResult> UploadResume(
        IFormFile file)
    {
        if (!IsStudentLoggedIn())
        {
            return RedirectToAction(
                "Login",
                "Account");
        }

        var profile =
            await _studentApiService
                .GetMyProfileAsync();

        if (profile == null)
        {
            return RedirectToAction(
                nameof(CompleteProfile));
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

        if (!result.Success)
        {
            TempData["Error"] =
                result.Message;

            return RedirectToAction(
                nameof(Resume));
        }

        TempData["Success"] =
            "Resume uploaded and text extracted successfully.";

        return RedirectToAction(
            nameof(Resume));
    }

    // --------------------------------------------------
    // EXTRACT SKILLS
    // --------------------------------------------------

    [HttpPost]
    public async Task<IActionResult> ExtractSkills()
    {
        if (!IsStudentLoggedIn())
        {
            return RedirectToAction(
                "Login",
                "Account");
        }

        var profile =
            await _studentApiService
                .GetMyProfileAsync();

        if (profile == null)
        {
            return RedirectToAction(
                nameof(CompleteProfile));
        }

        var result =
            await _studentApiService
                .ExtractSkillsAsync(
                    profile.StudentId);

        if (result == null)
        {
            TempData["Error"] =
                "Unable to extract skills.";

            return RedirectToAction(
                nameof(Resume));
        }

        TempData["Success"] =
            $"Skill extraction completed. " +
            $"{result.TotalSkillsDetected} skills detected.";

        return RedirectToAction(
            nameof(Resume));
    }

    // --------------------------------------------------
    // AI JOB RECOMMENDATIONS
    // --------------------------------------------------

    [HttpGet]
    public async Task<IActionResult> Recommendations()
    {
        if (!IsStudentLoggedIn())
        {
            return RedirectToAction(
                "Login",
                "Account");
        }

        var profile =
            await _studentApiService
                .GetMyProfileAsync();

        if (profile == null)
        {
            return RedirectToAction(
                nameof(CompleteProfile));
        }

        if (profile.Skills == null ||
            profile.Skills.Count == 0)
        {
            TempData["Error"] =
                "Upload your resume and extract skills before viewing job recommendations.";

            return RedirectToAction(
                nameof(Resume));
        }

        var recommendations =
            await _studentApiService
                .GetJobRecommendationsAsync(
                    profile.StudentId);

        if (recommendations == null)
        {
            ViewBag.Error =
                "No job recommendations are available right now.";

            return View(
                new JobRecommendationResponse());
        }

        return View(recommendations);
    }

    // --------------------------------------------------
    // APPLY FOR JOB
    // POST: /StudentDashboard/Apply
    // --------------------------------------------------

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Apply(
        int jobId)
    {
        if (!IsStudentLoggedIn())
        {
            return RedirectToAction(
                "Login",
                "Account");
        }

        var profile =
            await _studentApiService
                .GetMyProfileAsync();

        if (profile == null)
        {
            return RedirectToAction(
                nameof(CompleteProfile));
        }

        var result =
            await _studentApiService
                .ApplyForJobAsync(
                    profile.StudentId,
                    jobId);

        if (result.Success)
        {
            TempData["Success"] =
                result.Message;

            return RedirectToAction(
                nameof(MyApplications));
        }

        TempData["Error"] =
            result.Message;

        return RedirectToAction(
            nameof(Recommendations));
    }

    // --------------------------------------------------
    // MY APPLICATIONS
    // --------------------------------------------------

    [HttpGet]
    public async Task<IActionResult> MyApplications()
    {
        if (!IsStudentLoggedIn())
        {
            return RedirectToAction(
                "Login",
                "Account");
        }

        var profile =
            await _studentApiService
                .GetMyProfileAsync();

        if (profile == null)
        {
            return RedirectToAction(
                nameof(CompleteProfile));
        }

        var applications =
            await _studentApiService
                .GetMyApplicationsAsync(
                    profile.StudentId);

        return View(applications);
    }

    // --------------------------------------------------
    // LOGIN CHECK
    // --------------------------------------------------

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
}


// ==================================================
// PAGE VIEW MODEL
// ==================================================

public class ResumePageViewModel
{
    public int StudentId { get; set; }

    public ResumeInfo? Resume { get; set; }

    public List<string> Skills { get; set; } =
        new();
}