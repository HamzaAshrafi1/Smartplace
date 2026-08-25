using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartPlace.API.Data;
using SmartPlace.API.Models;
using SmartPlace.API.Services;

namespace SmartPlace.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ApplicationsController : ControllerBase
{
    private readonly SmartPlaceDbContext _context;
    private readonly JobEligibilityService
        _eligibilityService;

    public ApplicationsController(
        SmartPlaceDbContext context,
        JobEligibilityService eligibilityService)
    {
        _context = context;
        _eligibilityService =
            eligibilityService;
    }

    // ==================================================
    // GET ALL APPLICATIONS
    // ==================================================

    [HttpGet]
    [Authorize(
        Roles =
            "Admin,Recruiter,PlacementOfficer")]
    public async Task<
        ActionResult<IEnumerable<Application>>>
        GetApplications()
    {
        var applications =
            await _context.Applications
                .Include(a => a.Student)
                .ThenInclude(s =>
                    s!.Department)
                .Include(a => a.Job)
                .ThenInclude(j =>
                    j!.Company)
                .OrderByDescending(
                    a => a.AppliedDate)
                .ToListAsync();

        // Recruiter should only see applications
        // belonging to their own company.
        if (User.IsInRole("Recruiter"))
        {
            var userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            applications =
                applications
                    .Where(a =>
                        a.Job?.Company?
                            .RecruiterUserId ==
                        userId)
                    .ToList();
        }

        return Ok(applications);
    }

    // ==================================================
    // GET APPLICATION
    // ==================================================

    [HttpGet("{id:int}")]
    [Authorize(
        Roles =
            "Admin,Student,Recruiter,PlacementOfficer")]
    public async Task<IActionResult>
        GetApplication(int id)
    {
        var application =
            await _context.Applications
                .Include(a => a.Student)
                .ThenInclude(s =>
                    s!.Department)
                .Include(a => a.Job)
                .ThenInclude(j =>
                    j!.Company)
                .FirstOrDefaultAsync(a =>
                    a.ApplicationId == id);

        if (application == null)
        {
            return NotFound(new
            {
                message =
                    "Application not found."
            });
        }

        if (User.IsInRole("Student") &&
            !StudentOwns(application))
        {
            return Forbid();
        }

        if (User.IsInRole("Recruiter") &&
            !RecruiterOwns(application))
        {
            return Forbid();
        }

        return Ok(application);
    }

    // ==================================================
    // STUDENT APPLICATIONS
    // ==================================================

    [HttpGet("student/{studentId:int}")]
    [Authorize(
        Roles =
            "Admin,Student,Recruiter,PlacementOfficer")]
    public async Task<IActionResult>
        GetStudentApplications(
            int studentId)
    {
        var student =
            await _context.Students
                .FirstOrDefaultAsync(s =>
                    s.StudentId ==
                    studentId);

        if (student == null)
        {
            return NotFound(new
            {
                message =
                    "Student not found."
            });
        }

        if (User.IsInRole("Student") &&
            !OwnsStudent(student))
        {
            return Forbid();
        }

        var applications =
            await _context.Applications
                .Where(a =>
                    a.StudentId ==
                    studentId)
                .Include(a => a.Job)
                .ThenInclude(j =>
                    j!.Company)
                .OrderByDescending(
                    a => a.AppliedDate)
                .ToListAsync();

        if (User.IsInRole("Recruiter"))
        {
            var userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            applications =
                applications
                    .Where(a =>
                        a.Job?.Company?
                            .RecruiterUserId ==
                        userId)
                    .ToList();
        }

        return Ok(applications);
    }

    // ==================================================
    // APPLY
    // ==================================================

    [HttpPost]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult>
        CreateApplication(
            Application application)
    {
        var student =
            await _context.Students
                .Include(s => s.Department)
                .FirstOrDefaultAsync(s =>
                    s.StudentId ==
                    application.StudentId);

        if (student == null)
        {
            return BadRequest(new
            {
                message =
                    "Student not found."
            });
        }

        if (!OwnsStudent(student))
        {
            return Forbid();
        }

        var job =
            await _context.Jobs
                .Include(j => j.Company)
                .Include(j =>
                    j.RequiredDepartment)
                .FirstOrDefaultAsync(j =>
                    j.JobId ==
                    application.JobId);

        if (job == null)
        {
            return BadRequest(new
            {
                message =
                    "Job not found."
            });
        }

        if (!string.Equals(
                job.Status,
                "Published",
                StringComparison
                    .OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                message =
                    "This job is not accepting applications."
            });
        }

        if (job.ApplicationDeadline
                .HasValue &&
            job.ApplicationDeadline.Value <=
                DateTime.UtcNow)
        {
            return BadRequest(new
            {
                message =
                    "The application deadline has passed."
            });
        }

        var eligibility =
            _eligibilityService.Evaluate(
                student,
                job);

        if (!eligibility.IsEligible)
        {
            return BadRequest(new
            {
                message =
                    "Student does not meet the job eligibility requirements.",

                reasons =
                    eligibility.Reasons
            });
        }

        var alreadyApplied =
            await _context.Applications
                .AnyAsync(a =>
                    a.StudentId ==
                    application.StudentId
                    &&
                    a.JobId ==
                    application.JobId);

        if (alreadyApplied)
        {
            return BadRequest(new
            {
                message =
                    "Student has already applied for this job."
            });
        }

        application.AppliedDate =
            DateTime.UtcNow;

        application.Status =
            "Applied";

        application.Remarks = null;

        _context.Applications.Add(
            application);

        await _context.SaveChangesAsync();

        var created =
            await _context.Applications
                .Include(a => a.Student)
                .Include(a => a.Job)
                .ThenInclude(j =>
                    j!.Company)
                .FirstAsync(a =>
                    a.ApplicationId ==
                    application.ApplicationId);

        return CreatedAtAction(
            nameof(GetApplication),
            new
            {
                id =
                    created.ApplicationId
            },
            created);
    }

    // ==================================================
    // UPDATE APPLICATION STATUS
    // ==================================================

    [HttpPut("{id:int}/status")]
    [Authorize(
        Roles =
            "Admin,Recruiter,PlacementOfficer")]
    public async Task<IActionResult>
        UpdateApplicationStatus(
            int id,
            [FromBody] string status)
    {
        var application =
            await _context.Applications
                .Include(a => a.Job)
                .ThenInclude(j =>
                    j!.Company)
                .FirstOrDefaultAsync(a =>
                    a.ApplicationId == id);

        if (application == null)
        {
            return NotFound(new
            {
                message =
                    "Application not found."
            });
        }

        if (User.IsInRole("Recruiter") &&
            !RecruiterOwns(application))
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(
            status))
        {
            return BadRequest(new
            {
                message =
                    "Application status is required."
            });
        }

        string[] allowedStatuses =
        {
            "Applied",
            "Shortlisted",
            "Interview",
            "Selected",
            "Rejected"
        };

        var validStatus =
            allowedStatuses.FirstOrDefault(
                value =>
                    string.Equals(
                        value,
                        status,
                        StringComparison
                            .OrdinalIgnoreCase));

        if (validStatus == null)
        {
            return BadRequest(new
            {
                message =
                    "Invalid application status.",

                allowedStatuses
            });
        }

        application.Status =
            validStatus;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // ==================================================
    // DELETE
    // ==================================================

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult>
        DeleteApplication(int id)
    {
        var application =
            await _context.Applications
                .FindAsync(id);

        if (application == null)
        {
            return NotFound(new
            {
                message =
                    "Application not found."
            });
        }

        _context.Applications.Remove(
            application);

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // ==================================================
    // OWNERSHIP
    // ==================================================

    private bool OwnsStudent(
        Student student)
    {
        var userId =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        return
            !string.IsNullOrWhiteSpace(
                userId)
            &&
            student.ApplicationUserId ==
            userId;
    }

    private bool StudentOwns(
        Application application)
    {
        return
            application.Student != null &&
            OwnsStudent(
                application.Student);
    }

    private bool RecruiterOwns(
        Application application)
    {
        var userId =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        return
            !string.IsNullOrWhiteSpace(
                userId)
            &&
            application.Job?.Company?
                .RecruiterUserId ==
            userId;
    }
}