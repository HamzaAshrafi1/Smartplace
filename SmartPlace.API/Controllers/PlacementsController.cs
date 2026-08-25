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
public class PlacementsController : ControllerBase
{
    private readonly SmartPlaceDbContext _context;

    public PlacementsController(
        SmartPlaceDbContext context)
    {
        _context = context;
    }

    // ==================================================
    // GET ALL PLACEMENTS
    // Admin / Placement Officer
    // ==================================================

    [HttpGet]
    [Authorize(
        Roles = "Admin,PlacementOfficer")]
    public async Task<ActionResult<IEnumerable<Placement>>>
        GetPlacements()
    {
        var placements =
            await _context.Placements
                .Include(p => p.Student)
                .ThenInclude(s => s!.Department)
                .Include(p => p.Company)
                .OrderByDescending(
                    p => p.PlacementId)
                .ToListAsync();

        return Ok(placements);
    }

    // ==================================================
    // GET PLACEMENT BY ID
    // ==================================================

    [HttpGet("{id:int}")]
    [Authorize(
        Roles = "Admin,PlacementOfficer,Student")]
    public async Task<IActionResult>
        GetPlacement(int id)
    {
        var placement =
            await _context.Placements
                .Include(p => p.Student)
                .ThenInclude(s => s!.Department)
                .Include(p => p.Company)
                .FirstOrDefaultAsync(
                    p => p.PlacementId == id);

        if (placement == null)
        {
            return NotFound(new
            {
                message =
                    "Placement record not found."
            });
        }

        if (User.IsInRole("Student") &&
            !OwnsStudent(
                placement.Student))
        {
            return Forbid();
        }

        return Ok(placement);
    }

    // ==================================================
    // GET PLACEMENT BY STUDENT
    // ==================================================

    [HttpGet("student/{studentId:int}")]
    [Authorize(
        Roles = "Admin,PlacementOfficer,Student")]
    public async Task<IActionResult>
        GetPlacementByStudent(
            int studentId)
    {
        var student =
            await _context.Students
                .FirstOrDefaultAsync(
                    s => s.StudentId ==
                         studentId);

        if (student == null)
        {
            return NotFound(new
            {
                message =
                    "Student not found."
            });
        }

        if (User.IsInRole("Student") &&
            !OwnsStudent(student))
        {
            return Forbid();
        }

        var placement =
            await _context.Placements
                .Include(p => p.Student)
                .ThenInclude(s => s!.Department)
                .Include(p => p.Company)
                .FirstOrDefaultAsync(
                    p => p.StudentId ==
                         studentId);

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

    // ==================================================
    // CREATE PLACEMENT
    // Admin / Placement Officer
    // ==================================================

    [HttpPost]
    [Authorize(
        Roles = "Admin,PlacementOfficer")]
    public async Task<IActionResult>
        CreatePlacement(
            Placement placement)
    {
        var student =
            await _context.Students
                .FirstOrDefaultAsync(
                    s => s.StudentId ==
                         placement.StudentId);

        if (student == null)
        {
            return BadRequest(new
            {
                message =
                    "Student not found."
            });
        }

        var company =
            await _context.Companies
                .FirstOrDefaultAsync(
                    c => c.CompanyId ==
                         placement.CompanyId);

        if (company == null)
        {
            return BadRequest(new
            {
                message =
                    "Company not found."
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

        // --------------------------------------------------
        // Student must actually have been selected for
        // a job belonging to this company.
        // --------------------------------------------------

        var selectedApplication =
            await _context.Applications
                .Include(a => a.Job)
                .FirstOrDefaultAsync(a =>
                    a.StudentId ==
                        placement.StudentId
                    &&
                    a.Job != null
                    &&
                    a.Job.CompanyId ==
                        placement.CompanyId
                    &&
                    a.Status ==
                        "Selected");

        if (selectedApplication == null)
        {
            return BadRequest(new
            {
                message =
                    "This student has not been selected for a job at the specified company."
            });
        }

        var alreadyPlaced =
            await _context.Placements
                .AnyAsync(p =>
                    p.StudentId ==
                    placement.StudentId);

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

        placement.Status =
            "Placed";

        _context.Placements.Add(
            placement);

        await _context.SaveChangesAsync();

        var created =
            await _context.Placements
                .Include(p => p.Student)
                .ThenInclude(s => s!.Department)
                .Include(p => p.Company)
                .FirstAsync(p =>
                    p.PlacementId ==
                    placement.PlacementId);

        return CreatedAtAction(
            nameof(GetPlacement),
            new
            {
                id =
                    created.PlacementId
            },
            created);
    }

    // ==================================================
    // UPDATE PLACEMENT
    // ==================================================

    [HttpPut("{id:int}")]
    [Authorize(
        Roles = "Admin,PlacementOfficer")]
    public async Task<IActionResult>
        UpdatePlacement(
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

        var existing =
            await _context.Placements
                .FindAsync(id);

        if (existing == null)
        {
            return NotFound(new
            {
                message =
                    "Placement record not found."
            });
        }

        var studentExists =
            await _context.Students
                .AnyAsync(s =>
                    s.StudentId ==
                    placement.StudentId);

        if (!studentExists)
        {
            return BadRequest(new
            {
                message =
                    "Student not found."
            });
        }

        var company =
            await _context.Companies
                .FirstOrDefaultAsync(c =>
                    c.CompanyId ==
                    placement.CompanyId);

        if (company == null)
        {
            return BadRequest(new
            {
                message =
                    "Company not found."
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
                    "Placement requires an approved company."
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

        string[] allowedStatuses =
        {
            "Placed",
            "Joined",
            "Declined",
            "Cancelled"
        };

        var validStatus =
            allowedStatuses
                .FirstOrDefault(value =>
                    string.Equals(
                        value,
                        placement.Status,
                        StringComparison
                            .OrdinalIgnoreCase));

        if (validStatus == null)
        {
            return BadRequest(new
            {
                message =
                    "Invalid placement status.",

                allowedStatuses
            });
        }

        existing.OfferedPackage =
            placement.OfferedPackage;

        existing.JoiningDate =
            placement.JoiningDate;

        existing.Status =
            validStatus;

        existing.OfferLetterUrl =
            placement.OfferLetterUrl;

        existing.StudentId =
            placement.StudentId;

        existing.CompanyId =
            placement.CompanyId;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // ==================================================
    // DELETE PLACEMENT
    // ==================================================

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult>
        DeletePlacement(int id)
    {
        var placement =
            await _context.Placements
                .FindAsync(id);

        if (placement == null)
        {
            return NotFound(new
            {
                message =
                    "Placement record not found."
            });
        }

        _context.Placements.Remove(
            placement);

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // ==================================================
    // OWNERSHIP
    // ==================================================

    private bool OwnsStudent(
        Student? student)
    {
        if (student == null)
        {
            return false;
        }

        var userId =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        return
            !string.IsNullOrWhiteSpace(
                userId)
            &&
            student.ApplicationUserId ==
            userId;
    }
}