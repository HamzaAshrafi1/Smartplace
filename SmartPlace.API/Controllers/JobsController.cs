using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartPlace.API.Data;
using SmartPlace.API.Models;

namespace SmartPlace.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JobsController : ControllerBase
{
    private readonly SmartPlaceDbContext _context;

    public JobsController(SmartPlaceDbContext context)
    {
        _context = context;
    }

    // GET: api/Jobs
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Job>>> GetJobs()
    {
        var jobs = await _context.Jobs
            .Include(j => j.Company)
            .OrderByDescending(j => j.PostedDate)
            .ToListAsync();

        return Ok(jobs);
    }

    // GET: api/Jobs/1
    [HttpGet("{id}")]
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

    // GET: api/Jobs/company/1
    [HttpGet("company/{companyId}")]
    public async Task<ActionResult<IEnumerable<Job>>> GetJobsByCompany(int companyId)
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

    // POST: api/Jobs
    [HttpPost]
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

        if (company.ApprovalStatus.ToLower() != "approved")
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
            job.ApplicationDeadline.Value < DateTime.UtcNow)
        {
            return BadRequest(new
            {
                message = "Application deadline cannot be in the past."
            });
        }

        job.Title = job.Title.Trim();
        job.PostedDate = DateTime.UtcNow;

        if (string.IsNullOrWhiteSpace(job.Status))
        {
            job.Status = "Pending";
        }

        _context.Jobs.Add(job);
        await _context.SaveChangesAsync();

        var createdJob = await _context.Jobs
            .Include(j => j.Company)
            .FirstAsync(j => j.JobId == job.JobId);

        return CreatedAtAction(
            nameof(GetJob),
            new { id = createdJob.JobId },
            createdJob
        );
    }

    // PUT: api/Jobs/1
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateJob(int id, Job job)
    {
        if (id != job.JobId)
        {
            return BadRequest(new
            {
                message = "Job ID in URL does not match JobId in request body."
            });
        }

        var existingJob = await _context.Jobs.FindAsync(id);

        if (existingJob == null)
        {
            return NotFound(new
            {
                message = "Job not found."
            });
        }

        var companyExists = await _context.Companies
            .AnyAsync(c => c.CompanyId == job.CompanyId);

        if (!companyExists)
        {
            return BadRequest(new
            {
                message = "Invalid CompanyId."
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

        existingJob.Title = job.Title.Trim();
        existingJob.Description = job.Description;
        existingJob.Package = job.Package;
        existingJob.MinimumCGPA = job.MinimumCGPA;
        existingJob.MaximumBacklogs = job.MaximumBacklogs;
        existingJob.GraduationYear = job.GraduationYear;
        existingJob.Location = job.Location;
        existingJob.EmploymentType = job.EmploymentType;
        existingJob.ApplicationDeadline = job.ApplicationDeadline;
        existingJob.Status = job.Status;
        existingJob.CompanyId = job.CompanyId;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/Jobs/1
    [HttpDelete("{id}")]
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
                message = "Cannot delete job because applications are associated with it."
            });
        }

        _context.Jobs.Remove(job);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}