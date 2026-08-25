using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartPlace.API.Data;
using SmartPlace.API.Models;

namespace SmartPlace.API.Controllers;

[ApiController]
[Route("api/AdminUsers")]
[Authorize(Roles = "Admin")]
public class AdminUsersController : ControllerBase
{
    private readonly SmartPlaceDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminUsersController(
        SmartPlaceDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // ==================================================
    // GET ALL REGISTERED USERS
    // GET: api/AdminUsers
    // ==================================================

    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _userManager.Users
            .OrderBy(u => u.FullName)
            .ToListAsync();

        var result =
            new List<AdminUserInfo>();

        foreach (var user in users)
        {
            var roles =
                await _userManager
                    .GetRolesAsync(user);

            var student =
                await _context.Students
                    .FirstOrDefaultAsync(s =>
                        s.ApplicationUserId ==
                        user.Id);

            var company =
                await _context.Companies
                    .FirstOrDefaultAsync(c =>
                        c.RecruiterUserId ==
                        user.Id);

            var role =
                roles.FirstOrDefault()
                ?? "Unknown";

            var canDelete =
                role == "Student"
                || role == "Recruiter";

            string? deleteBlockedReason =
                null;

            if (role == "Admin")
            {
                canDelete = false;

                deleteBlockedReason =
                    "Admin accounts are protected.";
            }
            else if (role ==
                     "PlacementOfficer")
            {
                canDelete = false;

                deleteBlockedReason =
                    "Placement Officer accounts are protected.";
            }
            else if (role == "Student" &&
                     student != null)
            {
                var hasApplications =
                    await _context.Applications
                        .AnyAsync(a =>
                            a.StudentId ==
                            student.StudentId);

                var hasPlacement =
                    await _context.Placements
                        .AnyAsync(p =>
                            p.StudentId ==
                            student.StudentId);

                if (hasApplications)
                {
                    canDelete = false;

                    deleteBlockedReason =
                        "Student has application history.";
                }
                else if (hasPlacement)
                {
                    canDelete = false;

                    deleteBlockedReason =
                        "Student has a placement record.";
                }
            }
            else if (role == "Recruiter" &&
                     company != null)
            {
                var hasJobs =
                    await _context.Jobs
                        .AnyAsync(j =>
                            j.CompanyId ==
                            company.CompanyId);

                var hasPlacements =
                    await _context.Placements
                        .AnyAsync(p =>
                            p.CompanyId ==
                            company.CompanyId);

                if (hasJobs)
                {
                    canDelete = false;

                    deleteBlockedReason =
                        "Recruiter's company has job history.";
                }
                else if (hasPlacements)
                {
                    canDelete = false;

                    deleteBlockedReason =
                        "Recruiter's company has placement history.";
                }
            }

            result.Add(
                new AdminUserInfo
                {
                    UserId =
                        user.Id,

                    FullName =
                        user.FullName,

                    Email =
                        user.Email
                        ?? string.Empty,

                    Role =
                        role,

                    StudentId =
                        student?.StudentId,

                    CompanyId =
                        company?.CompanyId,

                    CompanyName =
                        company?.Name,

                    CompanyApprovalStatus =
                        company?
                            .ApprovalStatus,

                    CanDelete =
                        canDelete,

                    DeleteBlockedReason =
                        deleteBlockedReason
                });
        }

        return Ok(result);
    }

    // ==================================================
    // DELETE REGISTERED USER
    // DELETE: api/AdminUsers/{userId}
    // ==================================================

    [HttpDelete("{userId}")]
    public async Task<IActionResult>
        DeleteUser(string userId)
    {
        var loggedInUserId =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(
                userId))
        {
            return BadRequest(new
            {
                message =
                    "User ID is required."
            });
        }

        if (userId == loggedInUserId)
        {
            return BadRequest(new
            {
                message =
                    "You cannot delete your own Admin account."
            });
        }

        var user =
            await _userManager
                .FindByIdAsync(userId);

        if (user == null)
        {
            return NotFound(new
            {
                message =
                    "Registered user not found."
            });
        }

        var roles =
            await _userManager
                .GetRolesAsync(user);

        if (roles.Contains("Admin"))
        {
            return BadRequest(new
            {
                message =
                    "Admin accounts cannot be deleted through User Management."
            });
        }

        if (roles.Contains(
                "PlacementOfficer"))
        {
            return BadRequest(new
            {
                message =
                    "Placement Officer accounts cannot be deleted through User Management."
            });
        }

        await using var transaction =
            await _context.Database
                .BeginTransactionAsync();

        try
        {
            // ==================================================
            // STUDENT ACCOUNT
            // ==================================================

            if (roles.Contains("Student"))
            {
                var student =
                    await _context.Students
                        .Include(s => s.Resume)
                        .Include(s =>
                            s.StudentSkills)
                        .FirstOrDefaultAsync(s =>
                            s.ApplicationUserId ==
                            user.Id);

                if (student != null)
                {
                    var hasApplications =
                        await _context.Applications
                            .AnyAsync(a =>
                                a.StudentId ==
                                student.StudentId);

                    if (hasApplications)
                    {
                        return BadRequest(new
                        {
                            message =
                                "Cannot delete this student because application records exist."
                        });
                    }

                    var hasPlacement =
                        await _context.Placements
                            .AnyAsync(p =>
                                p.StudentId ==
                                student.StudentId);

                    if (hasPlacement)
                    {
                        return BadRequest(new
                        {
                            message =
                                "Cannot delete this student because a placement record exists."
                        });
                    }

                    // Delete physical resume file if present.
                    if (student.Resume != null &&
                        !string.IsNullOrWhiteSpace(
                            student.Resume.FilePath) &&
                        System.IO.File.Exists(
                            student.Resume.FilePath))
                    {
                        try
                        {
                            System.IO.File.Delete(
                                student.Resume.FilePath);
                        }
                        catch
                        {
                            // Database deletion should not fail
                            // merely because the physical file
                            // could not be removed.
                        }
                    }

                    // StudentSkills and Resume are configured
                    // with cascade delete.
                    _context.Students.Remove(
                        student);

                    await _context
                        .SaveChangesAsync();
                }
            }

            // ==================================================
            // RECRUITER ACCOUNT
            // ==================================================

            if (roles.Contains("Recruiter"))
            {
                var company =
                    await _context.Companies
                        .FirstOrDefaultAsync(c =>
                            c.RecruiterUserId ==
                            user.Id);

                if (company != null)
                {
                    var hasJobs =
                        await _context.Jobs
                            .AnyAsync(j =>
                                j.CompanyId ==
                                company.CompanyId);

                    if (hasJobs)
                    {
                        return BadRequest(new
                        {
                            message =
                                "Cannot delete this recruiter because their company has job records."
                        });
                    }

                    var hasPlacements =
                        await _context.Placements
                            .AnyAsync(p =>
                                p.CompanyId ==
                                company.CompanyId);

                    if (hasPlacements)
                    {
                        return BadRequest(new
                        {
                            message =
                                "Cannot delete this recruiter because their company has placement records."
                        });
                    }

                    _context.Companies.Remove(
                        company);

                    await _context
                        .SaveChangesAsync();
                }
            }

            // ==================================================
            // DELETE IDENTITY ACCOUNT
            // ==================================================

            var deleteResult =
                await _userManager
                    .DeleteAsync(user);

            if (!deleteResult.Succeeded)
            {
                await transaction
                    .RollbackAsync();

                var errors =
                    deleteResult.Errors
                        .Select(e =>
                            e.Description)
                        .ToList();

                return BadRequest(new
                {
                    message =
                        "Unable to delete the registered account.",

                    errors
                });
            }

            await transaction
                .CommitAsync();

            return Ok(new
            {
                message =
                    "User account deleted successfully."
            });
        }
        catch (Exception)
        {
            await transaction
                .RollbackAsync();

            return StatusCode(
                StatusCodes
                    .Status500InternalServerError,
                new
                {
                    message =
                        "An unexpected error occurred while deleting the user."
                });
        }
    }
}


// ==================================================
// RESPONSE DTO
// ==================================================

public class AdminUserInfo
{
    public string UserId { get; set; } =
        string.Empty;

    public string FullName { get; set; } =
        string.Empty;

    public string Email { get; set; } =
        string.Empty;

    public string Role { get; set; } =
        string.Empty;

    public int? StudentId { get; set; }

    public int? CompanyId { get; set; }

    public string? CompanyName
    { get; set; }

    public string? CompanyApprovalStatus
    { get; set; }

    public bool CanDelete { get; set; }

    public string? DeleteBlockedReason
    { get; set; }
}