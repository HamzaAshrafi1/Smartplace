using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartPlace.API.Data;
using SmartPlace.API.Models;

namespace SmartPlace.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InterviewRoundsController : ControllerBase
{
    private readonly SmartPlaceDbContext _context;

    public InterviewRoundsController(SmartPlaceDbContext context)
    {
        _context = context;
    }

    // GET: api/InterviewRounds
    [HttpGet]
    public async Task<ActionResult<IEnumerable<InterviewRound>>> GetInterviewRounds()
    {
        var interviews = await _context.InterviewRounds
            .Include(i => i.Application)
            .ThenInclude(a => a.Student)
            .Include(i => i.Application)
            .ThenInclude(a => a.Job)
            .OrderBy(i => i.ScheduledDate)
            .ToListAsync();

        return Ok(interviews);
    }

    // GET: api/InterviewRounds/5
    [HttpGet("{id}")]
    public async Task<ActionResult<InterviewRound>> GetInterviewRound(int id)
    {
        var interview = await _context.InterviewRounds
            .Include(i => i.Application)
            .ThenInclude(a => a.Student)
            .Include(i => i.Application)
            .ThenInclude(a => a.Job)
            .FirstOrDefaultAsync(i => i.InterviewRoundId == id);

        if (interview == null)
        {
            return NotFound(new
            {
                message = "Interview round not found."
            });
        }

        return Ok(interview);
    }

    // GET: api/InterviewRounds/application/1
    [HttpGet("application/{applicationId}")]
    public async Task<ActionResult<IEnumerable<InterviewRound>>> GetByApplication(int applicationId)
    {
        var interviews = await _context.InterviewRounds
            .Where(i => i.ApplicationId == applicationId)
            .OrderBy(i => i.ScheduledDate)
            .ToListAsync();

        return Ok(interviews);
    }

    // POST: api/InterviewRounds
    [HttpPost]
    public async Task<ActionResult<InterviewRound>> CreateInterviewRound(
        InterviewRound interviewRound)
    {
        var applicationExists = await _context.Applications
            .AnyAsync(a => a.ApplicationId == interviewRound.ApplicationId);

        if (!applicationExists)
        {
            return BadRequest(new
            {
                message = "Application not found."
            });
        }

        _context.InterviewRounds.Add(interviewRound);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetInterviewRound),
            new { id = interviewRound.InterviewRoundId },
            interviewRound);
    }

    // PUT: api/InterviewRounds/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateInterviewRound(
        int id,
        InterviewRound interviewRound)
    {
        if (id != interviewRound.InterviewRoundId)
        {
            return BadRequest(new
            {
                message = "Invalid interview ID."
            });
        }

        var existingInterview = await _context.InterviewRounds
            .FindAsync(id);

        if (existingInterview == null)
        {
            return NotFound(new
            {
                message = "Interview round not found."
            });
        }

        existingInterview.RoundName = interviewRound.RoundName;
        existingInterview.ScheduledDate = interviewRound.ScheduledDate;
        existingInterview.Mode = interviewRound.Mode;
        existingInterview.LocationOrLink = interviewRound.LocationOrLink;
        existingInterview.Status = interviewRound.Status;
        existingInterview.Result = interviewRound.Result;
        existingInterview.Remarks = interviewRound.Remarks;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/InterviewRounds/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteInterviewRound(int id)
    {
        var interview = await _context.InterviewRounds
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