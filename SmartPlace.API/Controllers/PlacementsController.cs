using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartPlace.API.Data;
using SmartPlace.API.Models;

namespace SmartPlace.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlacementsController : ControllerBase
{
    private readonly SmartPlaceDbContext _context;

    public PlacementsController(SmartPlaceDbContext context)
    {
        _context = context;
    }

    // GET: api/Placements
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Placement>>> GetPlacements()
    {
        var placements = await _context.Placements
            .Include(p => p.Student)
            .Include(p => p.Company)
            .OrderByDescending(p => p.PlacementId)
            .ToListAsync();

        return Ok(placements);
    }

    // GET: api/Placements/1
    [HttpGet("{id}")]
    public async Task<ActionResult<Placement>> GetPlacement(int id)
    {
        var placement = await _context.Placements
            .Include(p => p.Student)
            .Include(p => p.Company)
            .FirstOrDefaultAsync(p => p.PlacementId == id);

        if (placement == null)
        {
            return NotFound(new
            {
                message = "Placement record not found."
            });
        }

        return Ok(placement);
    }

    // GET: api/Placements/student/1
    [HttpGet("student/{studentId}")]
    public async Task<ActionResult<Placement>> GetPlacementByStudent(int studentId)
    {
        var placement = await _context.Placements
            .Include(p => p.Student)
            .Include(p => p.Company)
            .FirstOrDefaultAsync(p => p.StudentId == studentId);

        if (placement == null)
        {
            return NotFound(new
            {
                message = "No placement record found for this student."
            });
        }

        return Ok(placement);
    }

    // POST: api/Placements
    [HttpPost]
    public async Task<ActionResult<Placement>> CreatePlacement(Placement placement)
    {
        var studentExists = await _context.Students
            .AnyAsync(s => s.StudentId == placement.StudentId);

        if (!studentExists)
        {
            return BadRequest(new
            {
                message = "Student not found."
            });
        }

        var companyExists = await _context.Companies
            .AnyAsync(c => c.CompanyId == placement.CompanyId);

        if (!companyExists)
        {
            return BadRequest(new
            {
                message = "Company not found."
            });
        }

        var alreadyPlaced = await _context.Placements
            .AnyAsync(p => p.StudentId == placement.StudentId);

        if (alreadyPlaced)
        {
            return BadRequest(new
            {
                message = "Student already has a placement record."
            });
        }

        if (placement.OfferedPackage < 0)
        {
            return BadRequest(new
            {
                message = "Offered package cannot be negative."
            });
        }

        if (string.IsNullOrWhiteSpace(placement.Status))
        {
            placement.Status = "Placed";
        }

        _context.Placements.Add(placement);

        await _context.SaveChangesAsync();

        var createdPlacement = await _context.Placements
            .Include(p => p.Student)
            .Include(p => p.Company)
            .FirstAsync(p => p.PlacementId == placement.PlacementId);

        return CreatedAtAction(
            nameof(GetPlacement),
            new { id = createdPlacement.PlacementId },
            createdPlacement
        );
    }

    // PUT: api/Placements/1
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePlacement(
        int id,
        Placement placement)
    {
        if (id != placement.PlacementId)
        {
            return BadRequest(new
            {
                message = "Placement ID in URL does not match PlacementId in request body."
            });
        }

        var existingPlacement = await _context.Placements
            .FindAsync(id);

        if (existingPlacement == null)
        {
            return NotFound(new
            {
                message = "Placement record not found."
            });
        }

        var studentExists = await _context.Students
            .AnyAsync(s => s.StudentId == placement.StudentId);

        if (!studentExists)
        {
            return BadRequest(new
            {
                message = "Student not found."
            });
        }

        var companyExists = await _context.Companies
            .AnyAsync(c => c.CompanyId == placement.CompanyId);

        if (!companyExists)
        {
            return BadRequest(new
            {
                message = "Company not found."
            });
        }

        var duplicateStudentPlacement = await _context.Placements
            .AnyAsync(p =>
                p.PlacementId != id &&
                p.StudentId == placement.StudentId);

        if (duplicateStudentPlacement)
        {
            return BadRequest(new
            {
                message = "Student already has another placement record."
            });
        }

        existingPlacement.OfferedPackage = placement.OfferedPackage;
        existingPlacement.JoiningDate = placement.JoiningDate;
        existingPlacement.Status = placement.Status;
        existingPlacement.OfferLetterUrl = placement.OfferLetterUrl;
        existingPlacement.StudentId = placement.StudentId;
        existingPlacement.CompanyId = placement.CompanyId;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/Placements/1
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePlacement(int id)
    {
        var placement = await _context.Placements
            .FindAsync(id);

        if (placement == null)
        {
            return NotFound(new
            {
                message = "Placement record not found."
            });
        }

        _context.Placements.Remove(placement);

        await _context.SaveChangesAsync();

        return NoContent();
    }
}