using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartPlace.API.Data;
using SmartPlace.API.Models;

namespace SmartPlace.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CompaniesController : ControllerBase
{
    private readonly SmartPlaceDbContext _context;

    public CompaniesController(SmartPlaceDbContext context)
    {
        _context = context;
    }

    // GET: api/Companies
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Company>>> GetCompanies()
    {
        var companies = await _context.Companies
            .OrderBy(c => c.Name)
            .ToListAsync();

        return Ok(companies);
    }

    // GET: api/Companies/1
    [HttpGet("{id}")]
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

    // POST: api/Companies
    [HttpPost]
    public async Task<ActionResult<Company>> CreateCompany(Company company)
    {
        if (string.IsNullOrWhiteSpace(company.Name))
        {
            return BadRequest(new
            {
                message = "Company name is required."
            });
        }

        var exists = await _context.Companies
            .AnyAsync(c => c.Name.ToLower() == company.Name.ToLower());

        if (exists)
        {
            return BadRequest(new
            {
                message = "Company already exists."
            });
        }

        company.Name = company.Name.Trim();

        if (string.IsNullOrWhiteSpace(company.ApprovalStatus))
        {
            company.ApprovalStatus = "Pending";
        }

        _context.Companies.Add(company);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetCompany),
            new { id = company.CompanyId },
            company
        );
    }

    // PUT: api/Companies/1
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCompany(int id, Company company)
    {
        if (id != company.CompanyId)
        {
            return BadRequest(new
            {
                message = "Company ID in URL does not match CompanyId in request body."
            });
        }

        var existingCompany = await _context.Companies.FindAsync(id);

        if (existingCompany == null)
        {
            return NotFound(new
            {
                message = "Company not found."
            });
        }

        var duplicateExists = await _context.Companies
            .AnyAsync(c =>
                c.CompanyId != id &&
                c.Name.ToLower() == company.Name.ToLower());

        if (duplicateExists)
        {
            return BadRequest(new
            {
                message = "Another company with this name already exists."
            });
        }

        existingCompany.Name = company.Name.Trim();
        existingCompany.Industry = company.Industry;
        existingCompany.Location = company.Location;
        existingCompany.Website = company.Website;
        existingCompany.Description = company.Description;
        existingCompany.ApprovalStatus = company.ApprovalStatus;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/Companies/1
    [HttpDelete("{id}")]
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
                message = "Cannot delete company because jobs are associated with it."
            });
        }

        _context.Companies.Remove(company);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}