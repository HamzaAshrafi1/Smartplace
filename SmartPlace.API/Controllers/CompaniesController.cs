using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartPlace.API.Data;
using SmartPlace.API.Models;

namespace SmartPlace.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CompaniesController : ControllerBase
{
    private readonly SmartPlaceDbContext _context;

    public CompaniesController(
        SmartPlaceDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize(
        Roles = "Admin,Student,Recruiter,PlacementOfficer")]
    public async Task<ActionResult<IEnumerable<Company>>>
        GetCompanies()
    {
        IQueryable<Company> query =
            _context.Companies;

        if (User.IsInRole("Recruiter"))
        {
            var userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            query = query.Where(c =>
                c.RecruiterUserId == userId);
        }

        var companies =
            await query
                .OrderBy(c => c.Name)
                .ToListAsync();

        return Ok(companies);
    }

    [HttpGet("{id:int}")]
    [Authorize(
        Roles = "Admin,Student,Recruiter,PlacementOfficer")]
    public async Task<ActionResult<Company>>
        GetCompany(int id)
    {
        var company =
            await _context.Companies
                .FirstOrDefaultAsync(
                    c => c.CompanyId == id);

        if (company == null)
        {
            return NotFound(new
            {
                message = "Company not found."
            });
        }

        if (User.IsInRole("Recruiter") &&
            !RecruiterOwns(company))
        {
            return Forbid();
        }

        return Ok(company);
    }

    [HttpPost]
    [Authorize(Roles = "Recruiter")]
    public async Task<ActionResult<Company>>
        CreateCompany(Company company)
    {
        var userId =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var existingOwnedCompany =
            await _context.Companies
                .AnyAsync(c =>
                    c.RecruiterUserId == userId);

        if (existingOwnedCompany)
        {
            return BadRequest(new
            {
                message =
                    "This recruiter already has a registered company."
            });
        }

        if (string.IsNullOrWhiteSpace(
            company.Name))
        {
            return BadRequest(new
            {
                message =
                    "Company name is required."
            });
        }

        var normalizedName =
            company.Name.Trim();

        var duplicate =
            await _context.Companies
                .AnyAsync(c =>
                    c.Name.ToLower() ==
                    normalizedName.ToLower());

        if (duplicate)
        {
            return BadRequest(new
            {
                message =
                    "Company already exists."
            });
        }

        company.Name =
            normalizedName;

        company.ApprovalStatus =
            "Pending";

        company.RecruiterUserId =
            userId;

        _context.Companies.Add(company);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetCompany),
            new
            {
                id = company.CompanyId
            },
            company);
    }

    [HttpPut("{id:int}")]
    [Authorize(
        Roles = "Recruiter,Admin,PlacementOfficer")]
    public async Task<IActionResult>
        UpdateCompany(
            int id,
            Company company)
    {
        if (id != company.CompanyId)
        {
            return BadRequest(new
            {
                message =
                    "Company ID does not match."
            });
        }

        var existing =
            await _context.Companies
                .FindAsync(id);

        if (existing == null)
        {
            return NotFound(new
            {
                message =
                    "Company not found."
            });
        }

        if (User.IsInRole("Recruiter") &&
            !RecruiterOwns(existing))
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(
            company.Name))
        {
            return BadRequest(new
            {
                message =
                    "Company name is required."
            });
        }

        var name =
            company.Name.Trim();

        var duplicate =
            await _context.Companies
                .AnyAsync(c =>
                    c.CompanyId != id &&
                    c.Name.ToLower() ==
                    name.ToLower());

        if (duplicate)
        {
            return BadRequest(new
            {
                message =
                    "Another company already uses this name."
            });
        }

        existing.Name = name;
        existing.Industry =
            company.Industry;
        existing.Location =
            company.Location;
        existing.Website =
            company.Website;
        existing.Description =
            company.Description;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPut("{id:int}/approval")]
    [Authorize(
        Roles = "Admin,PlacementOfficer")]
    public async Task<IActionResult>
        UpdateApprovalStatus(
            int id,
            [FromBody] string status)
    {
        var company =
            await _context.Companies
                .FindAsync(id);

        if (company == null)
        {
            return NotFound(new
            {
                message =
                    "Company not found."
            });
        }

        string[] allowed =
        {
            "Pending",
            "Approved",
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
                    "Invalid approval status.",
                allowedStatuses = allowed
            });
        }

        company.ApprovalStatus = valid;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult>
        DeleteCompany(int id)
    {
        var company =
            await _context.Companies
                .Include(c => c.Jobs)
                .FirstOrDefaultAsync(
                    c => c.CompanyId == id);

        if (company == null)
        {
            return NotFound();
        }

        if (company.Jobs.Any())
        {
            return BadRequest(new
            {
                message =
                    "Cannot delete a company with jobs."
            });
        }

        _context.Companies.Remove(company);

        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool RecruiterOwns(
        Company company)
    {
        var userId =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        return
            !string.IsNullOrWhiteSpace(userId) &&
            company.RecruiterUserId == userId;
    }
}