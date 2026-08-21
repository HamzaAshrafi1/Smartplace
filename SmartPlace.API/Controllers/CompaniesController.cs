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

    public CompaniesController(SmartPlaceDbContext context)
    {
        _context = context;
    }

    // --------------------------------------------------
    // GET ALL COMPANIES
    // GET: api/Companies
    // --------------------------------------------------

    [HttpGet]
    [Authorize(Roles = "Admin,Student,Recruiter,PlacementOfficer")]
    public async Task<ActionResult<IEnumerable<Company>>> GetCompanies()
    {
        var companies = await _context.Companies
            .OrderBy(c => c.Name)
            .ToListAsync();

        return Ok(companies);
    }

    // --------------------------------------------------
    // GET COMPANY BY ID
    // GET: api/Companies/1
    // --------------------------------------------------

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,Student,Recruiter,PlacementOfficer")]
    public async Task<ActionResult<Company>> GetCompany(int id)
    {
        var company = await _context.Companies
            .FirstOrDefaultAsync(c => c.CompanyId == id);

        if (company == null)
        {
            return NotFound(new
            {
                message = "Company not found."
            });
        }

        return Ok(company);
    }

    // --------------------------------------------------
    // CREATE COMPANY
    // Recruiter
    // POST: api/Companies
    // --------------------------------------------------

    [HttpPost]
    [Authorize(Roles = "Recruiter")]
    public async Task<ActionResult<Company>> CreateCompany(
        Company company)
    {
        if (string.IsNullOrWhiteSpace(company.Name))
        {
            return BadRequest(new
            {
                message = "Company name is required."
            });
        }

        var exists = await _context.Companies
            .AnyAsync(c =>
                c.Name.ToLower() ==
                company.Name.Trim().ToLower());

        if (exists)
        {
            return BadRequest(new
            {
                message = "Company already exists."
            });
        }

        company.Name = company.Name.Trim();

        // New companies must first be approved
        company.ApprovalStatus = "Pending";

        _context.Companies.Add(company);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetCompany),
            new { id = company.CompanyId },
            company);
    }

    // --------------------------------------------------
    // UPDATE COMPANY DETAILS
    // Recruiter / Admin / Placement Officer
    // PUT: api/Companies/1
    // --------------------------------------------------

    [HttpPut("{id}")]
    [Authorize(Roles = "Recruiter,Admin,PlacementOfficer")]
    public async Task<IActionResult> UpdateCompany(
        int id,
        Company company)
    {
        if (id != company.CompanyId)
        {
            return BadRequest(new
            {
                message =
                    "Company ID in URL does not match CompanyId in request body."
            });
        }

        var existingCompany = await _context.Companies
            .FindAsync(id);

        if (existingCompany == null)
        {
            return NotFound(new
            {
                message = "Company not found."
            });
        }

        if (string.IsNullOrWhiteSpace(company.Name))
        {
            return BadRequest(new
            {
                message = "Company name is required."
            });
        }

        var duplicateExists = await _context.Companies
            .AnyAsync(c =>
                c.CompanyId != id &&
                c.Name.ToLower() ==
                company.Name.Trim().ToLower());

        if (duplicateExists)
        {
            return BadRequest(new
            {
                message =
                    "Another company with this name already exists."
            });
        }

        existingCompany.Name = company.Name.Trim();
        existingCompany.Industry = company.Industry;
        existingCompany.Location = company.Location;
        existingCompany.Website = company.Website;
        existingCompany.Description = company.Description;

        // ApprovalStatus is NOT changed here.
        // It has a dedicated secure endpoint.

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // --------------------------------------------------
    // APPROVE / REJECT COMPANY
    // Admin / Placement Officer
    // PUT: api/Companies/1/approval
    // --------------------------------------------------

    [HttpPut("{id}/approval")]
    [Authorize(Roles = "Admin,PlacementOfficer")]
    public async Task<IActionResult> UpdateApprovalStatus(
        int id,
        [FromBody] string status)
    {
        var company = await _context.Companies
            .FindAsync(id);

        if (company == null)
        {
            return NotFound(new
            {
                message = "Company not found."
            });
        }

        string[] allowedStatuses =
        {
            "Pending",
            "Approved",
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
                message = "Invalid approval status.",
                allowedStatuses
            });
        }

        company.ApprovalStatus = validStatus;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // --------------------------------------------------
    // DELETE COMPANY
    // Admin
    // DELETE: api/Companies/1
    // --------------------------------------------------

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteCompany(int id)
    {
        var company = await _context.Companies
            .Include(c => c.Jobs)
            .FirstOrDefaultAsync(c => c.CompanyId == id);

        if (company == null)
        {
            return NotFound(new
            {
                message = "Company not found."
            });
        }

        if (company.Jobs.Any())
        {
            return BadRequest(new
            {
                message =
                    "Cannot delete company because jobs are associated with it."
            });
        }

        _context.Companies.Remove(company);

        await _context.SaveChangesAsync();

        return NoContent();
    }
}