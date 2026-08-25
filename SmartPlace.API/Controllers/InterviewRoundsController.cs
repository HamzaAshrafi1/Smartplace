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
public class InterviewRoundsController
    : ControllerBase
{
    private readonly SmartPlaceDbContext
        _context;

    public InterviewRoundsController(
        SmartPlaceDbContext context)
    {
        _context = context;
    }

    // ==================================================
    // GET ALL INTERVIEWS
    // ==================================================

    [HttpGet]
    [Authorize(
        Roles =
            "Admin,Recruiter,PlacementOfficer")]
    public async Task<IActionResult>
        GetInterviewRounds()
    {
        IQueryable<InterviewRound> query =
            _context.InterviewRounds
                .Include(i => i.Application)
                    .ThenInclude(a =>
                        a!.Student)
                    .ThenInclude(s =>
                        s!.Department)
                .Include(i => i.Application)
                    .ThenInclude(a =>
                        a!.Job)
                    .ThenInclude(j =>
                        j!.Company);

        if (User.IsInRole("Recruiter"))
        {
            var userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            query =
                query.Where(i =>
                    i.Application != null
                    &&
                    i.Application.Job != null
                    &&
                    i.Application.Job.Company != null
                    &&
                    i.Application.Job.Company
                        .RecruiterUserId ==
                    userId);
        }

        var interviews =
            await query
                .OrderBy(i =>
                    i.ScheduledDate)
                .ToListAsync();

        return Ok(interviews);
    }

    // ==================================================
    // GET INTERVIEW BY ID
    // ==================================================

    [HttpGet("{id:int}")]
    [Authorize(
        Roles =
            "Admin,Student,Recruiter,PlacementOfficer")]
    public async Task<IActionResult>
        GetInterviewRound(int id)
    {
        var interview =
            await _context.InterviewRounds
                .Include(i => i.Application)
                    .ThenInclude(a =>
                        a!.Student)
                    .ThenInclude(s =>
                        s!.Department)
                .Include(i => i.Application)
                    .ThenInclude(a =>
                        a!.Job)
                    .ThenInclude(j =>
                        j!.Company)
                .FirstOrDefaultAsync(i =>
                    i.InterviewRoundId ==
                    id);

        if (interview == null)
        {
            return NotFound(new
            {
                message =
                    "Interview round not found."
            });
        }

        if (User.IsInRole("Student") &&
            !StudentOwns(
                interview.Application))
        {
            return Forbid();
        }

        if (User.IsInRole("Recruiter") &&
            !RecruiterOwns(
                interview.Application))
        {
            return Forbid();
        }

        return Ok(interview);
    }

    // ==================================================
    // GET BY APPLICATION
    // ==================================================

    [HttpGet(
        "application/{applicationId:int}")]
    [Authorize(
        Roles =
            "Admin,Student,Recruiter,PlacementOfficer")]
    public async Task<IActionResult>
        GetByApplication(
            int applicationId)
    {
        var application =
            await _context.Applications
                .Include(a => a.Student)
                .Include(a => a.Job)
                    .ThenInclude(j =>
                        j!.Company)
                .FirstOrDefaultAsync(a =>
                    a.ApplicationId ==
                    applicationId);

        if (application == null)
        {
            return NotFound(new
            {
                message =
                    "Application not found."
            });
        }

        if (User.IsInRole("Student") &&
            !StudentOwns(application))
        {
            return Forbid();
        }

        if (User.IsInRole("Recruiter") &&
            !RecruiterOwns(application))
        {
            return Forbid();
        }

        var interviews =
            await _context.InterviewRounds
                .Where(i =>
                    i.ApplicationId ==
                    applicationId)
                .OrderBy(i =>
                    i.ScheduledDate)
                .ToListAsync();

        return Ok(interviews);
    }

    // ==================================================
    // CREATE INTERVIEW
    // ==================================================

    [HttpPost]
    [Authorize(
        Roles =
            "Recruiter,PlacementOfficer")]
    public async Task<IActionResult>
        CreateInterviewRound(
            InterviewRound interviewRound)
    {
        var application =
            await _context.Applications
                .Include(a => a.Job)
                    .ThenInclude(j =>
                        j!.Company)
                .FirstOrDefaultAsync(a =>
                    a.ApplicationId ==
                    interviewRound
                        .ApplicationId);

        if (application == null)
        {
            return BadRequest(new
            {
                message =
                    "Application not found."
            });
        }

        if (User.IsInRole("Recruiter") &&
            !RecruiterOwns(application))
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(
            interviewRound.RoundName))
        {
            return BadRequest(new
            {
                message =
                    "Round name is required."
            });
        }

        if (interviewRound.ScheduledDate <=
            DateTime.UtcNow)
        {
            return BadRequest(new
            {
                message =
                    "Interview date must be in the future."
            });
        }

        if (string.Equals(
                application.Status,
                "Rejected",
                StringComparison
                    .OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                message =
                    "An interview cannot be scheduled for a rejected application."
            });
        }

        if (string.Equals(
                application.Status,
                "Selected",
                StringComparison
                    .OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                message =
                    "This student has already been selected."
            });
        }

        interviewRound.RoundName =
            interviewRound
                .RoundName.Trim();

        interviewRound.Status =
            "Scheduled";

        interviewRound.Result =
            "Pending";

        _context.InterviewRounds
            .Add(interviewRound);

        application.Status =
            "Interview";

        await _context
            .SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetInterviewRound),
            new
            {
                id =
                    interviewRound
                        .InterviewRoundId
            },
            interviewRound);
    }

    // ==================================================
    // UPDATE INTERVIEW DETAILS
    // ==================================================

    [HttpPut("{id:int}")]
    [Authorize(
        Roles =
            "Recruiter,PlacementOfficer")]
    public async Task<IActionResult>
        UpdateInterviewRound(
            int id,
            InterviewRound interviewRound)
    {
        if (id !=
            interviewRound
                .InterviewRoundId)
        {
            return BadRequest(new
            {
                message =
                    "Interview ID in URL does not match request body."
            });
        }

        var existing =
            await _context.InterviewRounds
                .Include(i => i.Application)
                    .ThenInclude(a =>
                        a!.Job)
                    .ThenInclude(j =>
                        j!.Company)
                .FirstOrDefaultAsync(i =>
                    i.InterviewRoundId ==
                    id);

        if (existing == null)
        {
            return NotFound(new
            {
                message =
                    "Interview round not found."
            });
        }

        if (User.IsInRole("Recruiter") &&
            !RecruiterOwns(
                existing.Application))
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(
            interviewRound.RoundName))
        {
            return BadRequest(new
            {
                message =
                    "Round name is required."
            });
        }

        if (interviewRound.ScheduledDate <=
            DateTime.UtcNow)
        {
            return BadRequest(new
            {
                message =
                    "Interview date must be in the future."
            });
        }

        existing.RoundName =
            interviewRound
                .RoundName.Trim();

        existing.ScheduledDate =
            interviewRound
                .ScheduledDate;

        existing.Mode =
            interviewRound.Mode;

        existing.LocationOrLink =
            interviewRound
                .LocationOrLink;

        existing.Remarks =
            interviewRound.Remarks;

        await _context
            .SaveChangesAsync();

        return NoContent();
    }

    // ==================================================
    // UPDATE RESULT
    // ==================================================

    [HttpPut("{id:int}/result")]
    [Authorize(
        Roles =
            "Recruiter,PlacementOfficer")]
    public async Task<IActionResult>
        UpdateInterviewResult(
            int id,
            [FromBody] string result)
    {
        var interview =
            await _context.InterviewRounds
                .Include(i => i.Application)
                    .ThenInclude(a =>
                        a!.Job)
                    .ThenInclude(j =>
                        j!.Company)
                .FirstOrDefaultAsync(i =>
                    i.InterviewRoundId ==
                    id);

        if (interview == null)
        {
            return NotFound(new
            {
                message =
                    "Interview round not found."
            });
        }

        if (User.IsInRole("Recruiter") &&
            !RecruiterOwns(
                interview.Application))
        {
            return Forbid();
        }

        string[] allowedResults =
        {
            "Pending",
            "Passed",
            "Failed"
        };

        var validResult =
            allowedResults
                .FirstOrDefault(value =>
                    string.Equals(
                        value,
                        result,
                        StringComparison
                            .OrdinalIgnoreCase));

        if (validResult == null)
        {
            return BadRequest(new
            {
                message =
                    "Invalid interview result.",

                allowedResults
            });
        }

        interview.Result =
            validResult;

        if (validResult == "Pending")
        {
            interview.Status =
                "Scheduled";

            if (interview.Application != null)
            {
                interview.Application.Status =
                    "Interview";
            }
        }
        else
        {
            interview.Status =
                "Completed";

            if (interview.Application != null)
            {
                if (validResult ==
                    "Failed")
                {
                    interview.Application
                        .Status =
                        "Rejected";
                }
                else
                {
                    // Passed does NOT automatically
                    // mean final selection.
                    //
                    // Recruiter can schedule another
                    // round or explicitly mark the
                    // application Selected afterward.
                    interview.Application
                        .Status =
                        "Shortlisted";
                }
            }
        }

        await _context
            .SaveChangesAsync();

        return NoContent();
    }

    // ==================================================
    // DELETE INTERVIEW
    // ==================================================

    [HttpDelete("{id:int}")]
    [Authorize(
        Roles =
            "Admin,PlacementOfficer")]
    public async Task<IActionResult>
        DeleteInterviewRound(int id)
    {
        var interview =
            await _context.InterviewRounds
                .FindAsync(id);

        if (interview == null)
        {
            return NotFound(new
            {
                message =
                    "Interview round not found."
            });
        }

        _context.InterviewRounds
            .Remove(interview);

        await _context
            .SaveChangesAsync();

        return NoContent();
    }

    // ==================================================
    // OWNERSHIP
    // ==================================================

    private bool StudentOwns(
        Application? application)
    {
        if (application?.Student == null)
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
            application.Student
                .ApplicationUserId ==
            userId;
    }

    private bool RecruiterOwns(
        Application? application)
    {
        if (application?.Job?.Company ==
            null)
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
            application.Job.Company
                .RecruiterUserId ==
            userId;
    }
}