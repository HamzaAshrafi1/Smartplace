using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartPlace.API.Data;
using SmartPlace.API.Models;

namespace SmartPlace.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ApplicationsController : ControllerBase
{
    private readonly SmartPlaceDbContext _context;

    public ApplicationsController(SmartPlaceDbContext context)
    {
        _context = context;
    }

    // --------------------------------------------------
    // GET ALL APPLICATIONS
    // Admin / Recruiter / Placement Officer
    // GET: api/Applications
    // --------------------------------------------------

    [HttpGet]
    [Authorize(Roles = "Admin,Recruiter,PlacementOfficer")]
    public async Task<ActionResult<IEnumerable<Application>>> GetApplications()
    {
        var applications = await _context.Applications
            .Include(a => a.Student)
            .Include(a => a.Job)
            .ThenInclude(j => j!.Company)
            .OrderByDescending(a => a.AppliedDate)
            .ToListAsync();

        return Ok(applications);
    }

    // --------------------------------------------------
    // GET APPLICATION BY ID
    // GET: api/Applications/1
    // --------------------------------------------------

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,Student,Recruiter,PlacementOfficer")]
    public async Task<ActionResult<Application>> GetApplication(int id)
    {
        var application = await _context.Applications
            .Include(a => a.Student)
            .Include(a => a.Job)
            .ThenInclude(j => j!.Company)
            .FirstOrDefaultAsync(a => a.ApplicationId == id);

        if (application == null)
        {
            return NotFound(new
            {
                message = "Application not found."
            });
        }

        return Ok(application);
    }

    // --------------------------------------------------
    // GET APPLICATIONS FOR A STUDENT
    // GET: api/Applications/student/1
    // --------------------------------------------------

    [HttpGet("student/{studentId}")]
    [Authorize(Roles = "Admin,Student,Recruiter,PlacementOfficer")]
    public async Task<ActionResult<IEnumerable<Application>>> GetStudentApplications(
        int studentId)
    {
        var studentExists = await _context.Students
            .AnyAsync(s => s.StudentId == studentId);

        if (!studentExists)
        {
            return NotFound(new
            {
                message = "Student not found."
            });
        }

        var applications = await _context.Applications
            .Where(a => a.StudentId == studentId)
            .Include(a => a.Job)
            .ThenInclude(j => j!.Company)
            .OrderByDescending(a => a.AppliedDate)
            .ToListAsync();

        return Ok(applications);
    }

    // --------------------------------------------------
    // CREATE APPLICATION
    // Student
    // POST: api/Applications
    // --------------------------------------------------

    [HttpPost]
    [Authorize(Roles = "Student")]
    public async Task<ActionResult<Application>> CreateApplication(
        Application application)
    {
        var student = await _context.Students
            .FirstOrDefaultAsync(
                s => s.StudentId == application.StudentId);

        if (student == null)
        {
            return BadRequest(new
            {
                message = "Student not found."
            });
        }

        var job = await _context.Jobs
            .Include(j => j.Company)
            .FirstOrDefaultAsync(
                j => j.JobId == application.JobId);

        if (job == null)
        {
            return BadRequest(new
            {
                message = "Job not found."
            });
        }

        if (!string.Equals(
                job.Status,
                "Published",
                StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                message =
                    "This job is not accepting applications."
            });
        }

        if (student.CGPA < job.MinimumCGPA)
        {
            return BadRequest(new
            {
                message =
                    "Student does not meet the minimum CGPA requirement."
            });
        }

        if (student.Backlogs > job.MaximumBacklogs)
        {
            return BadRequest(new
            {
                message =
                    "Student does not meet the backlog requirement."
            });
        }

        if (student.GraduationYear != job.GraduationYear)
        {
            return BadRequest(new
            {
                message =
                    "Student does not meet the graduation year requirement."
            });
        }

        var alreadyApplied = await _context.Applications
            .AnyAsync(a =>
                a.StudentId == application.StudentId &&
                a.JobId == application.JobId);

        if (alreadyApplied)
        {
            return BadRequest(new
            {
                message =
                    "Student has already applied for this job."
            });
        }

        application.AppliedDate = DateTime.UtcNow;
        application.Status = "Applied";

        _context.Applications.Add(application);

        await _context.SaveChangesAsync();

        var createdApplication = await _context.Applications
            .Include(a => a.Student)
            .Include(a => a.Job)
            .ThenInclude(j => j!.Company)
            .FirstAsync(
                a => a.ApplicationId ==
                     application.ApplicationId);

        return CreatedAtAction(
            nameof(GetApplication),
            new
            {
                id = application.ApplicationId
            },
            createdApplication);
    }

    // --------------------------------------------------
    // UPDATE APPLICATION STATUS
    // Recruiter / Admin / Placement Officer
    // PUT: api/Applications/1/status
    // --------------------------------------------------

    [HttpPut("{id}/status")]
    [Authorize(Roles = "Admin,Recruiter,PlacementOfficer")]
    public async Task<IActionResult> UpdateApplicationStatus(
        int id,
        [FromBody] string status)
    {
        var application = await _context.Applications
            .FindAsync(id);

        if (application == null)
        {
            return NotFound(new
            {
                message = "Application not found."
            });
        }

        if (string.IsNullOrWhiteSpace(status))
        {
            return BadRequest(new
            {
                message = "Application status is required."
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

        var validStatus = allowedStatuses
            .FirstOrDefault(s =>
                string.Equals(
                    s,
                    status,
                    StringComparison.OrdinalIgnoreCase));

        if (validStatus == null)
        {
            return BadRequest(new
            {
                message = "Invalid application status.",
                allowedStatuses
            });
        }

        application.Status = validStatus;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // --------------------------------------------------
    // DELETE APPLICATION
    // Admin
    // DELETE: api/Applications/1
    // --------------------------------------------------

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteApplication(int id)
    {
        var application = await _context.Applications
            .FindAsync(id);

        if (application == null)
        {
            return NotFound(new
            {
                message = "Application not found."
            });
        }

        _context.Applications.Remove(application);

        await _context.SaveChangesAsync();

        return NoContent();
    }
}