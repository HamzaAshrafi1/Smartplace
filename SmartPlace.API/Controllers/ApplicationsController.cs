using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartPlace.API.Data;
using SmartPlace.API.Models;

namespace SmartPlace.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ApplicationsController : ControllerBase
{
    private readonly SmartPlaceDbContext _context;

    public ApplicationsController(SmartPlaceDbContext context)
    {
        _context = context;
    }

    // GET: api/Applications
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Application>>> GetApplications()
    {
        var applications = await _context.Applications
            .Include(a => a.Student)
            .Include(a => a.Job)
            .ThenInclude(j => j.Company)
            .OrderByDescending(a => a.AppliedDate)
            .ToListAsync();

        return Ok(applications);
    }

    // GET: api/Applications/1
    [HttpGet("{id}")]
    public async Task<ActionResult<Application>> GetApplication(int id)
    {
        var application = await _context.Applications
            .Include(a => a.Student)
            .Include(a => a.Job)
            .ThenInclude(j => j.Company)
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

    // GET: api/Applications/student/1
    [HttpGet("student/{studentId}")]
    public async Task<ActionResult<IEnumerable<Application>>> GetStudentApplications(int studentId)
    {
        var applications = await _context.Applications
            .Where(a => a.StudentId == studentId)
            .Include(a => a.Job)
            .ThenInclude(j => j.Company)
            .OrderByDescending(a => a.AppliedDate)
            .ToListAsync();

        return Ok(applications);
    }

    // POST: api/Applications
    [HttpPost]
    public async Task<ActionResult<Application>> CreateApplication(Application application)
    {
        var student = await _context.Students
            .FirstOrDefaultAsync(s => s.StudentId == application.StudentId);

        if (student == null)
        {
            return BadRequest(new
            {
                message = "Student not found."
            });
        }

        var job = await _context.Jobs
            .Include(j => j.Company)
            .FirstOrDefaultAsync(j => j.JobId == application.JobId);

        if (job == null)
        {
            return BadRequest(new
            {
                message = "Job not found."
            });
        }

        if (job.Status.ToLower() != "published")
        {
            return BadRequest(new
            {
                message = "This job is not accepting applications."
            });
        }

        if (student.CGPA < job.MinimumCGPA)
        {
            return BadRequest(new
            {
                message = "Student does not meet the minimum CGPA requirement."
            });
        }

        if (student.Backlogs > job.MaximumBacklogs)
        {
            return BadRequest(new
            {
                message = "Student does not meet the backlog requirement."
            });
        }

        if (student.GraduationYear != job.GraduationYear)
        {
            return BadRequest(new
            {
                message = "Student does not meet the graduation year requirement."
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
                message = "Student has already applied for this job."
            });
        }

        application.AppliedDate = DateTime.UtcNow;
        application.Status = "Applied";

        _context.Applications.Add(application);

        await _context.SaveChangesAsync();

        var createdApplication = await _context.Applications
            .Include(a => a.Student)
            .Include(a => a.Job)
            .ThenInclude(j => j.Company)
            .FirstAsync(a => a.ApplicationId == application.ApplicationId);

        return CreatedAtAction(
            nameof(GetApplication),
            new { id = application.ApplicationId },
            createdApplication
        );
    }

    // PUT: api/Applications/1/status
    [HttpPut("{id}/status")]
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

        application.Status = status;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/Applications/1
    [HttpDelete("{id}")]
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