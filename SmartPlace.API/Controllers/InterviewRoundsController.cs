using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartPlace.API.Data;
using SmartPlace.API.Models;

namespace SmartPlace.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InterviewRoundsController : ControllerBase
{
    private readonly SmartPlaceDbContext _context;

    public InterviewRoundsController(
        SmartPlaceDbContext context)
    {
        _context = context;
    }

    // --------------------------------------------------
    // GET ALL INTERVIEW ROUNDS
    // Recruiter / Placement Officer / Admin
    // GET: api/InterviewRounds
    // --------------------------------------------------

    [HttpGet]
    [Authorize(Roles = "Admin,Recruiter,PlacementOfficer")]
    public async Task<ActionResult<IEnumerable<InterviewRound>>> GetInterviewRounds()
    {
        var interviews = await _context.InterviewRounds
            .Include(i => i.Application)
            .ThenInclude(a => a!.Student)
            .Include(i => i.Application)
            .ThenInclude(a => a!.Job)
            .OrderBy(i => i.ScheduledDate)
            .ToListAsync();

        return Ok(interviews);
    }

    // --------------------------------------------------
    // GET INTERVIEW ROUND BY ID
    // GET: api/InterviewRounds/1
    // --------------------------------------------------

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,Student,Recruiter,PlacementOfficer")]
    public async Task<ActionResult<InterviewRound>> GetInterviewRound(
        int id)
    {
        var interview = await _context.InterviewRounds
            .Include(i => i.Application)
            .ThenInclude(a => a!.Student)
            .Include(i => i.Application)
            .ThenInclude(a => a!.Job)
            .FirstOrDefaultAsync(
                i => i.InterviewRoundId == id);

        if (interview == null)
        {
            return NotFound(new
            {
                message = "Interview round not found."
            });
        }

        return Ok(interview);
    }

    // --------------------------------------------------
    // GET INTERVIEW ROUNDS BY APPLICATION
    // GET: api/InterviewRounds/application/1
    // --------------------------------------------------

    [HttpGet("application/{applicationId}")]
    [Authorize(Roles = "Admin,Student,Recruiter,PlacementOfficer")]
    public async Task<ActionResult<IEnumerable<InterviewRound>>> GetByApplication(
        int applicationId)
    {
        var applicationExists =
            await _context.Applications
                .AnyAsync(
                    a => a.ApplicationId == applicationId);

        if (!applicationExists)
        {
            return NotFound(new
            {
                message = "Application not found."
            });
        }

        var interviews = await _context.InterviewRounds
            .Where(i =>
                i.ApplicationId == applicationId)
            .OrderBy(i => i.ScheduledDate)
            .ToListAsync();

        return Ok(interviews);
    }

    // --------------------------------------------------
    // CREATE INTERVIEW ROUND
    // Recruiter / Placement Officer
    // POST: api/InterviewRounds
    // --------------------------------------------------

    [HttpPost]
    [Authorize(Roles = "Recruiter,PlacementOfficer")]
    public async Task<ActionResult<InterviewRound>> CreateInterviewRound(
        InterviewRound interviewRound)
    {
        var application = await _context.Applications
            .FirstOrDefaultAsync(
                a => a.ApplicationId ==
                     interviewRound.ApplicationId);

        if (application == null)
        {
            return BadRequest(new
            {
                message = "Application not found."
            });
        }

        if (string.IsNullOrWhiteSpace(
            interviewRound.RoundName))
        {
            return BadRequest(new
            {
                message = "Round name is required."
            });
        }

        if (interviewRound.ScheduledDate <= DateTime.UtcNow)
        {
            return BadRequest(new
            {
                message =
                    "Interview date must be in the future."
            });
        }

        interviewRound.Status = "Scheduled";

        if (string.IsNullOrWhiteSpace(
            interviewRound.Result))
        {
            interviewRound.Result = "Pending";
        }

        _context.InterviewRounds.Add(
            interviewRound);

        // Automatically update application status
        application.Status = "Interview";

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetInterviewRound),
            new
            {
                id = interviewRound.InterviewRoundId
            },
            interviewRound);
    }

    // --------------------------------------------------
    // UPDATE INTERVIEW ROUND
    // Recruiter / Placement Officer
    // PUT: api/InterviewRounds/1
    // --------------------------------------------------

    [HttpPut("{id}")]
    [Authorize(Roles = "Recruiter,PlacementOfficer")]
    public async Task<IActionResult> UpdateInterviewRound(
        int id,
        InterviewRound interviewRound)
    {
        if (id != interviewRound.InterviewRoundId)
        {
            return BadRequest(new
            {
                message =
                    "Interview ID in URL does not match request body."
            });
        }

        var existingInterview =
            await _context.InterviewRounds
                .FindAsync(id);

        if (existingInterview == null)
        {
            return NotFound(new
            {
                message = "Interview round not found."
            });
        }

        existingInterview.RoundName =
            interviewRound.RoundName;

        existingInterview.ScheduledDate =
            interviewRound.ScheduledDate;

        existingInterview.Mode =
            interviewRound.Mode;

        existingInterview.LocationOrLink =
            interviewRound.LocationOrLink;

        existingInterview.Status =
            interviewRound.Status;

        existingInterview.Result =
            interviewRound.Result;

        existingInterview.Remarks =
            interviewRound.Remarks;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // --------------------------------------------------
    // UPDATE INTERVIEW RESULT
    // Recruiter / Placement Officer
    // PUT: api/InterviewRounds/1/result
    // --------------------------------------------------

    [HttpPut("{id}/result")]
    [Authorize(Roles = "Recruiter,PlacementOfficer")]
    public async Task<IActionResult> UpdateInterviewResult(
        int id,
        [FromBody] string result)
    {
        var interview =
            await _context.InterviewRounds
                .Include(i => i.Application)
                .FirstOrDefaultAsync(
                    i => i.InterviewRoundId == id);

        if (interview == null)
        {
            return NotFound(new
            {
                message = "Interview round not found."
            });
        }

        string[] allowedResults =
        {
            "Pending",
            "Passed",
            "Failed"
        };

        var validResult =
            allowedResults.FirstOrDefault(r =>
                string.Equals(
                    r,
                    result,
                    StringComparison.OrdinalIgnoreCase));

        if (validResult == null)
        {
            return BadRequest(new
            {
                message = "Invalid interview result.",
                allowedResults
            });
        }

        interview.Result = validResult;
        interview.Status = "Completed";

        if (interview.Application != null)
        {
            if (validResult == "Failed")
            {
                interview.Application.Status =
                    "Rejected";
            }
            else if (validResult == "Passed")
            {
                interview.Application.Status =
                    "Shortlisted";
            }
        }

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // --------------------------------------------------
    // DELETE INTERVIEW ROUND
    // Admin / Placement Officer
    // DELETE: api/InterviewRounds/1
    // --------------------------------------------------

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,PlacementOfficer")]
    public async Task<IActionResult> DeleteInterviewRound(
        int id)
    {
        var interview =
            await _context.InterviewRounds
                .FindAsync(id);

        if (interview == null)
        {
            return NotFound(new
            {
                message = "Interview round not found."
            });
        }

        _context.InterviewRounds.Remove(interview);

        await _context.SaveChangesAsync();

        return NoContent();
    }
}