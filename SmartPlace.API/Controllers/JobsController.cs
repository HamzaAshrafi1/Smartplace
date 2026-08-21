using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartPlace.API.Data;
using SmartPlace.API.Models;

namespace SmartPlace.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class JobsController : ControllerBase
{
    private readonly SmartPlaceDbContext _context;

    public JobsController(SmartPlaceDbContext context)
    {
        _context = context;
    }

    // --------------------------------------------------
    // GET ALL JOBS
    // All authenticated users
    // GET: api/Jobs
    // --------------------------------------------------

    [HttpGet]
    [Authorize(Roles = "Admin,Student,Recruiter,PlacementOfficer")]
    public async Task<ActionResult<IEnumerable<Job>>> GetJobs()
    {
        var jobs = await _context.Jobs
            .Include(j => j.Company)
            .OrderByDescending(j => j.PostedDate)
            .ToListAsync();

        return Ok(jobs);
    }

    // --------------------------------------------------
    // GET JOB BY ID
    // GET: api/Jobs/1
    // --------------------------------------------------

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,Student,Recruiter,PlacementOfficer")]
    public async Task<ActionResult<Job>> GetJob(int id)
    {
        var job = await _context.Jobs
            .Include(j => j.Company)
            .FirstOrDefaultAsync(j => j.JobId == id);

        if (job == null)
        {
            return NotFound(new
            {
                message = "Job not found."
            });
        }

        return Ok(job);
    }

    // --------------------------------------------------
    // GET JOBS BY COMPANY
    // GET: api/Jobs/company/1
    // --------------------------------------------------

    [HttpGet("company/{companyId}")]
    [Authorize(Roles = "Admin,Student,Recruiter,PlacementOfficer")]
    public async Task<ActionResult<IEnumerable<Job>>> GetJobsByCompany(
        int companyId)
    {
        var companyExists = await _context.Companies
            .AnyAsync(c => c.CompanyId == companyId);

        if (!companyExists)
        {
            return NotFound(new
            {
                message = "Company not found."
            });
        }

        var jobs = await _context.Jobs
            .Where(j => j.CompanyId == companyId)
            .Include(j => j.Company)
            .OrderByDescending(j => j.PostedDate)
            .ToListAsync();

        return Ok(jobs);
    }

    // --------------------------------------------------
    // CREATE JOB
    // Recruiter
    // POST: api/Jobs
    // --------------------------------------------------

    [HttpPost]
    [Authorize(Roles = "Recruiter")]
    public async Task<ActionResult<Job>> CreateJob(Job job)
    {
        var company = await _context.Companies
            .FirstOrDefaultAsync(c => c.CompanyId == job.CompanyId);

        if (company == null)
        {
            return BadRequest(new
            {
                message = "Invalid CompanyId. Company does not exist."
            });
        }

        if (!string.Equals(
                company.ApprovalStatus,
                "Approved",
                StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                message = "Only approved companies can post jobs."
            });
        }

        if (string.IsNullOrWhiteSpace(job.Title))
        {
            return BadRequest(new
            {
                message = "Job title is required."
            });
        }

        if (string.IsNullOrWhiteSpace(job.Description))
        {
            return BadRequest(new
            {
                message = "Job description is required."
            });
        }

        if (job.MinimumCGPA < 0 || job.MinimumCGPA > 10)
        {
            return BadRequest(new
            {
                message = "Minimum CGPA must be between 0 and 10."
            });
        }

        if (job.MaximumBacklogs < 0)
        {
            return BadRequest(new
            {
                message = "Maximum backlogs cannot be negative."
            });
        }

        if (job.ApplicationDeadline.HasValue &&
            job.ApplicationDeadline.Value <= DateTime.UtcNow)
        {
            return BadRequest(new
            {
                message = "Application deadline must be in the future."
            });
        }

        job.Title = job.Title.Trim();
        job.PostedDate = DateTime.UtcNow;

        // Recruiter creates job as Pending
        job.Status = "Pending";

        _context.Jobs.Add(job);

        await _context.SaveChangesAsync();

        var createdJob = await _context.Jobs
            .Include(j => j.Company)
            .FirstAsync(j => j.JobId == job.JobId);

        return CreatedAtAction(
            nameof(GetJob),
            new { id = createdJob.JobId },
            createdJob);
    }

    // --------------------------------------------------
    // UPDATE JOB DETAILS
    // Recruiter / Admin / Placement Officer
    // PUT: api/Jobs/1
    // --------------------------------------------------

    [HttpPut("{id}")]
    [Authorize(Roles = "Recruiter,Admin,PlacementOfficer")]
    public async Task<IActionResult> UpdateJob(
        int id,
        Job job)
    {
        if (id != job.JobId)
        {
            return BadRequest(new
            {
                message =
                    "Job ID in URL does not match JobId in request body."
            });
        }

        var existingJob = await _context.Jobs
            .FindAsync(id);

        if (existingJob == null)
        {
            return NotFound(new
            {
                message = "Job not found."
            });
        }

        var company = await _context.Companies
            .FirstOrDefaultAsync(c => c.CompanyId == job.CompanyId);

        if (company == null)
        {
            return BadRequest(new
            {
                message = "Invalid CompanyId."
            });
        }

        if (!string.Equals(
                company.ApprovalStatus,
                "Approved",
                StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                message =
                    "Job must belong to an approved company."
            });
        }

        if (string.IsNullOrWhiteSpace(job.Title))
        {
            return BadRequest(new
            {
                message = "Job title is required."
            });
        }

        if (string.IsNullOrWhiteSpace(job.Description))
        {
            return BadRequest(new
            {
                message = "Job description is required."
            });
        }

        if (job.MinimumCGPA < 0 || job.MinimumCGPA > 10)
        {
            return BadRequest(new
            {
                message =
                    "Minimum CGPA must be between 0 and 10."
            });
        }

        if (job.MaximumBacklogs < 0)
        {
            return BadRequest(new
            {
                message =
                    "Maximum backlogs cannot be negative."
            });
        }

        if (job.ApplicationDeadline.HasValue &&
            job.ApplicationDeadline.Value <= DateTime.UtcNow)
        {
            return BadRequest(new
            {
                message =
                    "Application deadline must be in the future."
            });
        }

        existingJob.Title = job.Title.Trim();
        existingJob.Description = job.Description;
        existingJob.Package = job.Package;
        existingJob.MinimumCGPA = job.MinimumCGPA;
        existingJob.MaximumBacklogs = job.MaximumBacklogs;
        existingJob.GraduationYear = job.GraduationYear;
        existingJob.Location = job.Location;
        existingJob.EmploymentType = job.EmploymentType;
        existingJob.ApplicationDeadline =
            job.ApplicationDeadline;
        existingJob.CompanyId = job.CompanyId;

        // Status is NOT changed here.
        // It has its own secure endpoint.

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // --------------------------------------------------
    // UPDATE JOB STATUS
    // Admin / Placement Officer
    // PUT: api/Jobs/1/status
    // --------------------------------------------------

    [HttpPut("{id}/status")]
    [Authorize(Roles = "Admin,PlacementOfficer")]
    public async Task<IActionResult> UpdateJobStatus(
        int id,
        [FromBody] string status)
    {
        var job = await _context.Jobs
            .FindAsync(id);

        if (job == null)
        {
            return NotFound(new
            {
                message = "Job not found."
            });
        }

        string[] allowedStatuses =
        {
            "Pending",
            "Published",
            "Closed",
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
                message = "Invalid job status.",
                allowedStatuses
            });
        }

        job.Status = validStatus;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // --------------------------------------------------
    // DELETE JOB
    // Admin
    // DELETE: api/Jobs/1
    // --------------------------------------------------

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteJob(int id)
    {
        var job = await _context.Jobs
            .Include(j => j.Applications)
            .FirstOrDefaultAsync(j => j.JobId == id);

        if (job == null)
        {
            return NotFound(new
            {
                message = "Job not found."
            });
        }

        if (job.Applications.Any())
        {
            return BadRequest(new
            {
                message =
                    "Cannot delete job because applications are associated with it."
            });
        }

        _context.Jobs.Remove(job);

        await _context.SaveChangesAsync();

        return NoContent();
    }
}