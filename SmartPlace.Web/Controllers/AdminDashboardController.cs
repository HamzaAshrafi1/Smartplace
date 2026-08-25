using Microsoft.AspNetCore.Mvc;
using SmartPlace.Web.Services;

namespace SmartPlace.Web.Controllers;

public class AdminDashboardController
    : Controller
{
    private readonly ManagementApiService
        _service;

    private readonly UserManagementApiService
        _userManagementService;

    public AdminDashboardController(
        ManagementApiService service,
        UserManagementApiService userManagementService)
    {
        _service =
            service;

        _userManagementService =
            userManagementService;
    }

    // ==================================================
    // DASHBOARD
    // ==================================================

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (!IsAdmin())
        {
            return LoginRedirect();
        }

        var model =
            new AdminDashboardViewModel
            {
                Students =
                    await _service
                        .GetStudentsAsync(),

                Companies =
                    await _service
                        .GetCompaniesAsync(),

                Jobs =
                    await _service
                        .GetJobsAsync(),

                Applications =
                    await _service
                        .GetApplicationsAsync(),

                Placements =
                    await _service
                        .GetPlacementsAsync(),

                Departments =
                    await _service
                        .GetDepartmentsAsync(),

                Skills =
                    await _service
                        .GetSkillsAsync()
            };

        return View(model);
    }

    // ==================================================
    // USER MANAGEMENT
    // ==================================================

    [HttpGet]
    public async Task<IActionResult> Users()
    {
        if (!IsAdmin())
        {
            return LoginRedirect();
        }

        var users =
            await _userManagementService
                .GetUsersAsync();

        return View(users);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult>
        DeleteUser(string userId)
    {
        if (!IsAdmin())
        {
            return LoginRedirect();
        }

        if (string.IsNullOrWhiteSpace(
            userId))
        {
            TempData["Error"] =
                "Invalid user account.";

            return RedirectToAction(
                nameof(Users));
        }

        var result =
            await _userManagementService
                .DeleteUserAsync(
                    userId);

        TempData[
            result.Success
                ? "Success"
                : "Error"] =
            result.Message;

        return RedirectToAction(
            nameof(Users));
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
        if (!IsAdmin())
        {
            return LoginRedirect();
        }

        Flash(
            await _service
                .UpdateCompanyApprovalAsync(
                    id,
                    status));

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
        if (!IsAdmin())
        {
            return LoginRedirect();
        }

        Flash(
            await _service
                .UpdateJobStatusAsync(
                    id,
                    status));

        return RedirectToAction(
            nameof(Index));
    }

    // ==================================================
    // CREATE DEPARTMENT
    // ==================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult>
        CreateDepartment(
            string name)
    {
        if (!IsAdmin())
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

        Flash(
            await _service
                .CreateDepartmentAsync(
                    name.Trim()));

        return RedirectToAction(
            nameof(Index));
    }

    // ==================================================
    // CREATE SKILL
    // ==================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult>
        CreateSkill(
            string name)
    {
        if (!IsAdmin())
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

        Flash(
            await _service
                .CreateSkillAsync(
                    name.Trim()));

        return RedirectToAction(
            nameof(Index));
    }

    // ==================================================
    // HELPERS
    // ==================================================

    private bool IsAdmin()
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
                "Admin",
                StringComparison
                    .OrdinalIgnoreCase);
    }

    private IActionResult
        LoginRedirect()
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
// ADMIN DASHBOARD VIEW MODEL
// ==================================================

public class AdminDashboardViewModel
{
    public List<ManagementStudent>
        Students
    { get; set; } = new();

    public List<ManagementCompany>
        Companies
    { get; set; } = new();

    public List<ManagementJob>
        Jobs
    { get; set; } = new();

    public List<ManagementApplication>
        Applications
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