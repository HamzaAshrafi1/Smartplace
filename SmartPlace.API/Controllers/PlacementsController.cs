using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartPlace.API.Data;
using SmartPlace.API.Models;

namespace SmartPlace.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PlacementsController : ControllerBase
{
    private readonly SmartPlaceDbContext _context;

    public PlacementsController(SmartPlaceDbContext context)
    {
        _context = context;
    }

    // --------------------------------------------------
    // GET ALL PLACEMENTS
    // Admin / Placement Officer
    // GET: api/Placements
    // --------------------------------------------------

    [HttpGet]
    [Authorize(Roles = "Admin,PlacementOfficer")]
    public async Task<ActionResult<IEnumerable<Placement>>> GetPlacements()
    {
        var placements = await _context.Placements
            .Include(p => p.Student)
            .Include(p => p.Company)
            .OrderByDescending(p => p.PlacementId)
            .ToListAsync();

        return Ok(placements);
    }

    // --------------------------------------------------
    // GET PLACEMENT BY ID
    // Admin / Placement Officer / Student
    // GET: api/Placements/1
    // --------------------------------------------------

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,PlacementOfficer,Student")]
    public async Task<ActionResult<Placement>> GetPlacement(int id)
    {
        var placement = await _context.Placements
            .Include(p => p.Student)
            .Include(p => p.Company)
            .FirstOrDefaultAsync(
                p => p.PlacementId == id);

        if (placement == null)
        {
            return NotFound(new
            {
                message = "Placement record not found."
            });
        }

        return Ok(placement);
    }

    // --------------------------------------------------
    // GET PLACEMENT BY STUDENT
    // Admin / Placement Officer / Student
    // GET: api/Placements/student/1
    // --------------------------------------------------

    [HttpGet("student/{studentId}")]
    [Authorize(Roles = "Admin,PlacementOfficer,Student")]
    public async Task<ActionResult<Placement>> GetPlacementByStudent(
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

        var placement = await _context.Placements
            .Include(p => p.Student)
            .Include(p => p.Company)
            .FirstOrDefaultAsync(
                p => p.StudentId == studentId);

        if (placement == null)
        {
            return NotFound(new
            {
                message =
                    "No placement record found for this student."
            });
        }

        return Ok(placement);
    }

    // --------------------------------------------------
    // CREATE PLACEMENT
    // Admin / Placement Officer
    // POST: api/Placements
    // --------------------------------------------------

    [HttpPost]
    [Authorize(Roles = "Admin,PlacementOfficer")]
    public async Task<ActionResult<Placement>> CreatePlacement(
        Placement placement)
    {
        var studentExists = await _context.Students
            .AnyAsync(
                s => s.StudentId == placement.StudentId);

        if (!studentExists)
        {
            return BadRequest(new
            {
                message = "Student not found."
            });
        }

        var company = await _context.Companies
            .FirstOrDefaultAsync(
                c => c.CompanyId == placement.CompanyId);

        if (company == null)
        {
            return BadRequest(new
            {
                message = "Company not found."
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
                    "Placement can only be recorded for an approved company."
            });
        }

        var alreadyPlaced = await _context.Placements
            .AnyAsync(
                p => p.StudentId == placement.StudentId);

        if (alreadyPlaced)
        {
            return BadRequest(new
            {
                message =
                    "Student already has a placement record."
            });
        }

        if (placement.OfferedPackage < 0)
        {
            return BadRequest(new
            {
                message =
                    "Offered package cannot be negative."
            });
        }

        // Placement status is controlled by the API
        // when the record is first created.
        placement.Status = "Placed";

        _context.Placements.Add(placement);

        await _context.SaveChangesAsync();

        var createdPlacement = await _context.Placements
            .Include(p => p.Student)
            .Include(p => p.Company)
            .FirstAsync(
                p => p.PlacementId ==
                     placement.PlacementId);

        return CreatedAtAction(
            nameof(GetPlacement),
            new
            {
                id = createdPlacement.PlacementId
            },
            createdPlacement);
    }

    // --------------------------------------------------
    // UPDATE PLACEMENT
    // Admin / Placement Officer
    // PUT: api/Placements/1
    // --------------------------------------------------

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,PlacementOfficer")]
    public async Task<IActionResult> UpdatePlacement(
        int id,
        Placement placement)
    {
        if (id != placement.PlacementId)
        {
            return BadRequest(new
            {
                message =
                    "Placement ID in URL does not match PlacementId in request body."
            });
        }

        var existingPlacement =
            await _context.Placements
                .FindAsync(id);

        if (existingPlacement == null)
        {
            return NotFound(new
            {
                message = "Placement record not found."
            });
        }

        var studentExists = await _context.Students
            .AnyAsync(
                s => s.StudentId == placement.StudentId);

        if (!studentExists)
        {
            return BadRequest(new
            {
                message = "Student not found."
            });
        }

        var company = await _context.Companies
            .FirstOrDefaultAsync(
                c => c.CompanyId == placement.CompanyId);

        if (company == null)
        {
            return BadRequest(new
            {
                message = "Company not found."
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
                    "Placement can only belong to an approved company."
            });
        }

        var duplicateStudentPlacement =
            await _context.Placements
                .AnyAsync(p =>
                    p.PlacementId != id &&
                    p.StudentId == placement.StudentId);

        if (duplicateStudentPlacement)
        {
            return BadRequest(new
            {
                message =
                    "Student already has another placement record."
            });
        }

        if (placement.OfferedPackage < 0)
        {
            return BadRequest(new
            {
                message =
                    "Offered package cannot be negative."
            });
        }

        if (string.IsNullOrWhiteSpace(
            placement.Status))
        {
            return BadRequest(new
            {
                message =
                    "Placement status is required."
            });
        }

        string[] allowedStatuses =
        {
            "Placed",
            "Joined",
            "Declined",
            "Cancelled"
        };

        var validStatus =
            allowedStatuses.FirstOrDefault(s =>
                string.Equals(
                    s,
                    placement.Status,
                    StringComparison.OrdinalIgnoreCase));

        if (validStatus == null)
        {
            return BadRequest(new
            {
                message = "Invalid placement status.",
                allowedStatuses
            });
        }

        existingPlacement.OfferedPackage =
            placement.OfferedPackage;

        existingPlacement.JoiningDate =
            placement.JoiningDate;

        existingPlacement.Status =
            validStatus;

        existingPlacement.OfferLetterUrl =
            placement.OfferLetterUrl;

        existingPlacement.StudentId =
            placement.StudentId;

        existingPlacement.CompanyId =
            placement.CompanyId;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // --------------------------------------------------
    // DELETE PLACEMENT
    // Admin
    // DELETE: api/Placements/1
    // --------------------------------------------------

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
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