using Microsoft.AspNetCore.Mvc;
using SmartPlace.Web.Services;

namespace SmartPlace.Web.Controllers;

public class RecruiterDashboardController
    : Controller
{
    private readonly ManagementApiService
        _service;

    public RecruiterDashboardController(
        ManagementApiService service)
    {
        _service = service;
    }

    // ==================================================
    // DASHBOARD
    // ==================================================

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (!IsRecruiter())
        {
            return LoginRedirect();
        }

        var companies =
            await _service
                .GetCompaniesAsync();

        var jobs =
            await _service
                .GetJobsAsync();

        var applications =
            await _service
                .GetApplicationsAsync();

        var interviews =
            await _service
                .GetInterviewsAsync();

        var departments =
            await _service
                .GetDepartmentsAsync();

        var selectedStudents =
            applications
                .Where(a =>
                    string.Equals(
                        a.Status,
                        "Selected",
                        StringComparison
                            .OrdinalIgnoreCase))
                .OrderBy(a =>
                    a.Student?.FullName)
                .ToList();

        var model =
            new RecruiterDashboardViewModel
            {
                Companies =
                    companies,

                Jobs =
                    jobs,

                Applications =
                    applications,

                Interviews =
                    interviews,

                Departments =
                    departments,

                SelectedStudents =
                    selectedStudents
            };

        return View(model);
    }

    // ==================================================
    // COMPANY
    // ==================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult>
        CreateCompany(
            ManagementCompanyRequest model)
    {
        if (!IsRecruiter())
        {
            return LoginRedirect();
        }

        var result =
            await _service
                .CreateCompanyAsync(
                    model);

        Flash(result);

        return RedirectToAction(
            nameof(Index));
    }

    // ==================================================
    // CREATE JOB
    // ==================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult>
        CreateJob(
            ManagementJobRequest model)
    {
        if (!IsRecruiter())
        {
            return LoginRedirect();
        }

        var currentYear =
            DateTime.Now.Year;

        if (model.MinimumTenthPercentage < 0 ||
            model.MinimumTenthPercentage > 100)
        {
            TempData["Error"] =
                "Minimum 10th percentage must be between 0 and 100.";

            return RedirectToAction(
                nameof(Index));
        }

        if (model.MinimumTwelfthPercentage < 0 ||
            model.MinimumTwelfthPercentage > 100)
        {
            TempData["Error"] =
                "Minimum 12th percentage must be between 0 and 100.";

            return RedirectToAction(
                nameof(Index));
        }

        if (model.MinimumCGPA < 0 ||
            model.MinimumCGPA > 10)
        {
            TempData["Error"] =
                "Minimum CGPA must be between 0 and 10.";

            return RedirectToAction(
                nameof(Index));
        }

        if (model.MaximumBacklogs < 0)
        {
            TempData["Error"] =
                "Maximum backlogs cannot be negative.";

            return RedirectToAction(
                nameof(Index));
        }

        if (model.GraduationYear <
            currentYear)
        {
            TempData["Error"] =
                "Graduation year cannot be earlier than the current year.";

            return RedirectToAction(
                nameof(Index));
        }

        if (model.RequiredDepartmentId <= 0)
        {
            TempData["Error"] =
                "Please select a required department.";

            return RedirectToAction(
                nameof(Index));
        }

        var result =
            await _service
                .CreateJobAsync(
                    model);

        Flash(result);

        return RedirectToAction(
            nameof(Index));
    }

    // ==================================================
    // APPLICATION STATUS
    // ==================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult>
        ApplicationStatus(
            int id,
            string status)
    {
        if (!IsRecruiter())
        {
            return LoginRedirect();
        }

        var result =
            await _service
                .UpdateApplicationStatusAsync(
                    id,
                    status);

        Flash(result);

        return RedirectToAction(
            nameof(Index));
    }

    // ==================================================
    // SCHEDULE INTERVIEW
    // ==================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult>
        ScheduleInterview(
            ManagementInterviewRequest model)
    {
        if (!IsRecruiter())
        {
            return LoginRedirect();
        }

        if (string.IsNullOrWhiteSpace(
            model.RoundName))
        {
            TempData["Error"] =
                "Interview round name is required.";

            return RedirectToAction(
                nameof(Index));
        }

        if (model.ScheduledDate <=
            DateTime.Now)
        {
            TempData["Error"] =
                "Interview date must be in the future.";

            return RedirectToAction(
                nameof(Index));
        }

        var result =
            await _service
                .CreateInterviewAsync(
                    model);

        Flash(result);

        return RedirectToAction(
            nameof(Index));
    }

    // ==================================================
    // INTERVIEW RESULT
    // ==================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult>
        InterviewResult(
            int id,
            string result)
    {
        if (!IsRecruiter())
        {
            return LoginRedirect();
        }

        var response =
            await _service
                .UpdateInterviewResultAsync(
                    id,
                    result);

        Flash(response);

        return RedirectToAction(
            nameof(Index));
    }

    // ==================================================
    // HELPERS
    // ==================================================

    private bool IsRecruiter()
    {
        var token =
            HttpContext.Session
                .GetString("JWToken");

        var role =
            HttpContext.Session
                .GetString("UserRole");

        return
            !string.IsNullOrWhiteSpace(
                token)
            &&
            string.Equals(
                role,
                "Recruiter",
                StringComparison
                    .OrdinalIgnoreCase);
    }

    private IActionResult LoginRedirect()
    {
        return RedirectToAction(
            "Login",
            "Account");
    }

    private void Flash(
        ApiActionResult result)
    {
        TempData[
            result.Success
                ? "Success"
                : "Error"] =
            result.Message;
    }
}


// ==================================================
// VIEW MODEL
// ==================================================

public class RecruiterDashboardViewModel
{
    public List<ManagementCompany>
        Companies
    { get; set; } = new();

    public List<ManagementJob>
        Jobs
    { get; set; } = new();

    public List<ManagementApplication>
        Applications
    { get; set; } = new();

    public List<ManagementInterview>
        Interviews
    { get; set; } = new();

    public List<DepartmentItem>
        Departments
    { get; set; } = new();

    public List<ManagementApplication>
        SelectedStudents
    { get; set; } = new();
}