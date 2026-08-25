using Microsoft.AspNetCore.Mvc;
using SmartPlace.Web.Services;

namespace SmartPlace.Web.Controllers;

public class PlacementDashboardController
    : Controller
{
    private readonly ManagementApiService
        _service;

    public PlacementDashboardController(
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
        if (!IsPlacementOfficer())
        {
            return LoginRedirect();
        }

        return View(
            await BuildModelAsync());
    }

    // ==================================================
    // COMPANY APPROVAL
    // ==================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult>
        CompanyApproval(
            int id,
            string status)
    {
        if (!IsPlacementOfficer())
        {
            return LoginRedirect();
        }

        var result =
            await _service
                .UpdateCompanyApprovalAsync(
                    id,
                    status);

        Flash(result);

        return RedirectToAction(
            nameof(Index));
    }

    // ==================================================
    // JOB STATUS
    // ==================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult>
        JobStatus(
            int id,
            string status)
    {
        if (!IsPlacementOfficer())
        {
            return LoginRedirect();
        }

        var result =
            await _service
                .UpdateJobStatusAsync(
                    id,
                    status);

        Flash(result);

        return RedirectToAction(
            nameof(Index));
    }

    // ==================================================
    // CREATE PLACEMENT
    // ==================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult>
        CreatePlacement(
            ManagementPlacementRequest model)
    {
        if (!IsPlacementOfficer())
        {
            return LoginRedirect();
        }

        if (model.StudentId <= 0)
        {
            TempData["Error"] =
                "Please select a student.";

            return RedirectToAction(
                nameof(Index));
        }

        if (model.CompanyId <= 0)
        {
            TempData["Error"] =
                "Please select a company.";

            return RedirectToAction(
                nameof(Index));
        }

        if (model.OfferedPackage < 0)
        {
            TempData["Error"] =
                "Offered package cannot be negative.";

            return RedirectToAction(
                nameof(Index));
        }

        var result =
            await _service
                .CreatePlacementAsync(
                    model);

        Flash(result);

        return RedirectToAction(
            nameof(Index));
    }

    // ==================================================
    // DEPARTMENT
    // ==================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult>
        CreateDepartment(
            string name)
    {
        if (!IsPlacementOfficer())
        {
            return LoginRedirect();
        }

        if (string.IsNullOrWhiteSpace(
            name))
        {
            TempData["Error"] =
                "Department name is required.";

            return RedirectToAction(
                nameof(Index));
        }

        var result =
            await _service
                .CreateDepartmentAsync(
                    name.Trim());

        Flash(result);

        return RedirectToAction(
            nameof(Index));
    }

    // ==================================================
    // SKILL
    // ==================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult>
        CreateSkill(
            string name)
    {
        if (!IsPlacementOfficer())
        {
            return LoginRedirect();
        }

        if (string.IsNullOrWhiteSpace(
            name))
        {
            TempData["Error"] =
                "Skill name is required.";

            return RedirectToAction(
                nameof(Index));
        }

        var result =
            await _service
                .CreateSkillAsync(
                    name.Trim());

        Flash(result);

        return RedirectToAction(
            nameof(Index));
    }

    // ==================================================
    // BUILD MODEL
    // ==================================================

    private async Task<PlacementDashboardViewModel>
        BuildModelAsync()
    {
        var companies =
            await _service
                .GetCompaniesAsync();

        var jobs =
            await _service
                .GetJobsAsync();

        var students =
            await _service
                .GetStudentsAsync();

        var applications =
            await _service
                .GetApplicationsAsync();

        var placements =
            await _service
                .GetPlacementsAsync();

        var departments =
            await _service
                .GetDepartmentsAsync();

        var skills =
            await _service
                .GetSkillsAsync();

        var selectedApplications =
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

        var placedStudentIds =
            placements
                .Select(p =>
                    p.StudentId)
                .ToHashSet();

        var pendingPlacements =
            selectedApplications
                .Where(a =>
                    !placedStudentIds.Contains(
                        a.StudentId))
                .ToList();

        return new PlacementDashboardViewModel
        {
            Companies =
                companies,

            Jobs =
                jobs,

            Students =
                students,

            Applications =
                applications,

            SelectedApplications =
                selectedApplications,

            PendingPlacements =
                pendingPlacements,

            Placements =
                placements,

            Departments =
                departments,

            Skills =
                skills
        };
    }

    // ==================================================
    // HELPERS
    // ==================================================

    private bool IsPlacementOfficer()
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
                "PlacementOfficer",
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

public class PlacementDashboardViewModel
{
    public List<ManagementCompany>
        Companies
    { get; set; } = new();

    public List<ManagementJob>
        Jobs
    { get; set; } = new();

    public List<ManagementStudent>
        Students
    { get; set; } = new();

    public List<ManagementApplication>
        Applications
    { get; set; } = new();

    public List<ManagementApplication>
        SelectedApplications
    { get; set; } = new();

    public List<ManagementApplication>
        PendingPlacements
    { get; set; } = new();

    public List<ManagementPlacement>
        Placements
    { get; set; } = new();

    public List<DepartmentItem>
        Departments
    { get; set; } = new();

    public List<ManagementSkill>
        Skills
    { get; set; } = new();
}