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
public class JobsController : ControllerBase
{
    private readonly SmartPlaceDbContext _context;
    private readonly JobEligibilityService _eligibility;

    public JobsController(
        SmartPlaceDbContext context,
        JobEligibilityService eligibility)
    {
        _context = context;
        _eligibility = eligibility;
    }

    [HttpGet]
    [Authorize(
        Roles = "Admin,Student,Recruiter,PlacementOfficer")]
    public async Task<IActionResult> GetJobs()
    {
        IQueryable<Job> query =
            _context.Jobs
                .Include(j => j.Company)
                .Include(j => j.RequiredDepartment);

        if (User.IsInRole("Student"))
        {
            query = query.Where(j =>
                j.Status == "Published");
        }

        if (User.IsInRole("Recruiter"))
        {
            var userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            query = query.Where(j =>
                j.Company != null &&
                j.Company.RecruiterUserId ==
                userId);
        }

        var jobs =
            await query
                .OrderByDescending(
                    j => j.PostedDate)
                .ToListAsync();

        return Ok(jobs);
    }

    // Student categorized job view
    [HttpGet("student/{studentId:int}/eligibility")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult>
        GetStudentJobEligibility(
            int studentId)
    {
        var student =
            await _context.Students
                .Include(s => s.Department)
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

        if (!OwnsStudent(student))
        {
            return Forbid();
        }

        var jobs =
            await _context.Jobs
                .Include(j => j.Company)
                .Include(j =>
                    j.RequiredDepartment)
                .Where(j =>
                    j.Status == "Published")
                .OrderByDescending(
                    j => j.PostedDate)
                .ToListAsync();

        var results =
            jobs.Select(job =>
            {
                var evaluation =
                    _eligibility.Evaluate(
                        student,
                        job);

                return new
                {
                    job.JobId,
                    job.Title,
                    company =
                        job.Company?.Name,
                    job.Package,
                    job.Location,
                    job.EmploymentType,
                    job.ApplicationDeadline,

                    requirements = new
                    {
                        department =
                            job.RequiredDepartment?.Name,

                        job.MinimumTenthPercentage,
                        job.MinimumTwelfthPercentage,
                        job.MinimumCGPA,
                        job.MaximumBacklogs,
                        job.GraduationYear
                    },

                    studentValues = new
                    {
                        department =
                            student.Department?.Name,

                        student.TenthPercentage,
                        student.TwelfthPercentage,
                        student.CGPA,
                        student.Backlogs,
                        student.GraduationYear
                    },

                    eligible =
                        evaluation.IsEligible,

                    evaluation.Reasons
                };
            })
            .ToList();

        return Ok(new
        {
            totalJobs =
                results.Count,

            eligibleCount =
                results.Count(x =>
                    x.eligible),

            notEligibleCount =
                results.Count(x =>
                    !x.eligible),

            jobs = results
        });
    }

    [HttpGet("{id:int}")]
    [Authorize(
        Roles = "Admin,Student,Recruiter,PlacementOfficer")]
    public async Task<IActionResult>
        GetJob(int id)
    {
        var job =
            await _context.Jobs
                .Include(j => j.Company)
                .Include(j =>
                    j.RequiredDepartment)
                .FirstOrDefaultAsync(j =>
                    j.JobId == id);

        if (job == null)
        {
            return NotFound(new
            {
                message = "Job not found."
            });
        }

        if (User.IsInRole("Recruiter") &&
            !RecruiterOwns(job))
        {
            return Forbid();
        }

        return Ok(job);
    }

    [HttpGet("company/{companyId:int}")]
    [Authorize(
        Roles = "Admin,Student,Recruiter,PlacementOfficer")]
    public async Task<IActionResult>
        GetJobsByCompany(
            int companyId)
    {
        var company =
            await _context.Companies
                .FirstOrDefaultAsync(c =>
                    c.CompanyId ==
                    companyId);

        if (company == null)
        {
            return NotFound(new
            {
                message =
                    "Company not found."
            });
        }

        if (User.IsInRole("Recruiter") &&
            !RecruiterOwns(company))
        {
            return Forbid();
        }

        var jobs =
            await _context.Jobs
                .Where(j =>
                    j.CompanyId ==
                    companyId)
                .Include(j => j.Company)
                .Include(j =>
                    j.RequiredDepartment)
                .ToListAsync();

        return Ok(jobs);
    }

    [HttpPost]
    [Authorize(Roles = "Recruiter")]
    public async Task<IActionResult>
        CreateJob(Job job)
    {
        var company =
            await _context.Companies
                .FirstOrDefaultAsync(c =>
                    c.CompanyId ==
                    job.CompanyId);

        if (company == null)
        {
            return BadRequest(new
            {
                message =
                    "Company does not exist."
            });
        }

        if (!RecruiterOwns(company))
        {
            return Forbid();
        }

        if (!string.Equals(
                company.ApprovalStatus,
                "Approved",
                StringComparison
                    .OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                message =
                    "Your company must be approved before posting jobs."
            });
        }

        var validationError =
            await ValidateJob(job);

        if (validationError != null)
        {
            return validationError;
        }

        job.Title =
            job.Title.Trim();

        job.PostedDate =
            DateTime.UtcNow;

        job.Status =
            "Pending";

        _context.Jobs.Add(job);

        await _context.SaveChangesAsync();

        return Ok(job);
    }

    [HttpPut("{id:int}")]
    [Authorize(
        Roles = "Recruiter,Admin,PlacementOfficer")]
    public async Task<IActionResult>
        UpdateJob(
            int id,
            Job job)
    {
        if (id != job.JobId)
        {
            return BadRequest(new
            {
                message =
                    "Job ID does not match."
            });
        }

        var existing =
            await _context.Jobs
                .Include(j => j.Company)
                .FirstOrDefaultAsync(j =>
                    j.JobId == id);

        if (existing == null)
        {
            return NotFound();
        }

        if (User.IsInRole("Recruiter") &&
            !RecruiterOwns(existing))
        {
            return Forbid();
        }

        var validationError =
            await ValidateJob(job);

        if (validationError != null)
        {
            return validationError;
        }

        existing.Title =
            job.Title.Trim();

        existing.Description =
            job.Description;

        existing.Package =
            job.Package;

        existing.MinimumTenthPercentage =
            job.MinimumTenthPercentage;

        existing.MinimumTwelfthPercentage =
            job.MinimumTwelfthPercentage;

        existing.MinimumCGPA =
            job.MinimumCGPA;

        existing.MaximumBacklogs =
            job.MaximumBacklogs;

        existing.GraduationYear =
            job.GraduationYear;

        existing.RequiredDepartmentId =
            job.RequiredDepartmentId;

        existing.Location =
            job.Location;

        existing.EmploymentType =
            job.EmploymentType;

        existing.ApplicationDeadline =
            job.ApplicationDeadline;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPut("{id:int}/status")]
    [Authorize(
        Roles = "Admin,PlacementOfficer")]
    public async Task<IActionResult>
        UpdateJobStatus(
            int id,
            [FromBody] string status)
    {
        var job =
            await _context.Jobs
                .FindAsync(id);

        if (job == null)
        {
            return NotFound();
        }

        string[] allowed =
        {
            "Pending",
            "Published",
            "Closed",
            "Rejected"
        };

        var valid =
            allowed.FirstOrDefault(s =>
                string.Equals(
                    s,
                    status,
                    StringComparison
                        .OrdinalIgnoreCase));

        if (valid == null)
        {
            return BadRequest(new
            {
                message =
                    "Invalid job status.",
                allowedStatuses = allowed
            });
        }

        job.Status = valid;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult>
        DeleteJob(int id)
    {
        var job =
            await _context.Jobs
                .Include(j =>
                    j.Applications)
                .FirstOrDefaultAsync(j =>
                    j.JobId == id);

        if (job == null)
        {
            return NotFound();
        }

        if (job.Applications.Any())
        {
            return BadRequest(new
            {
                message =
                    "Cannot delete job with applications."
            });
        }

        _context.Jobs.Remove(job);

        await _context.SaveChangesAsync();

        return NoContent();
    }

    private async Task<IActionResult?>
        ValidateJob(Job job)
    {
        if (string.IsNullOrWhiteSpace(
            job.Title))
        {
            return BadRequest(new
            {
                message =
                    "Job title is required."
            });
        }

        if (string.IsNullOrWhiteSpace(
            job.Description))
        {
            return BadRequest(new
            {
                message =
                    "Job description is required."
            });
        }

        if (job.MinimumTenthPercentage < 0 ||
            job.MinimumTenthPercentage > 100)
        {
            return BadRequest(new
            {
                message =
                    "Minimum 10th percentage must be between 0 and 100."
            });
        }

        if (job.MinimumTwelfthPercentage < 0 ||
            job.MinimumTwelfthPercentage > 100)
        {
            return BadRequest(new
            {
                message =
                    "Minimum 12th percentage must be between 0 and 100."
            });
        }

        if (job.MinimumCGPA < 0 ||
            job.MinimumCGPA > 10)
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

        if (job.GraduationYear <
            DateTime.UtcNow.Year)
        {
            return BadRequest(new
            {
                message =
                    "Graduation year cannot be earlier than the current year."
            });
        }

        var departmentExists =
            await _context.Departments
                .AnyAsync(d =>
                    d.DepartmentId ==
                    job.RequiredDepartmentId);

        if (!departmentExists)
        {
            return BadRequest(new
            {
                message =
                    "Required department is invalid."
            });
        }

        if (job.ApplicationDeadline.HasValue &&
            job.ApplicationDeadline.Value <=
            DateTime.UtcNow)
        {
            return BadRequest(new
            {
                message =
                    "Application deadline must be in the future."
            });
        }

        return null;
    }

    private bool RecruiterOwns(
        Company company)
    {
        var userId =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        return company.RecruiterUserId ==
               userId;
    }

    private bool RecruiterOwns(
        Job job)
    {
        return job.Company != null &&
               RecruiterOwns(
                   job.Company);
    }

    private bool OwnsStudent(
        Student student)
    {
        var userId =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        return student.ApplicationUserId ==
               userId;
    }
}