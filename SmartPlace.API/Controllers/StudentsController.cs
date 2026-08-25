using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartPlace.API.Data;
using SmartPlace.API.Models;

namespace SmartPlace.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StudentsController : ControllerBase
{
    private readonly SmartPlaceDbContext _context;
    private readonly UserManager<ApplicationUser>
        _userManager;

    public StudentsController(
        SmartPlaceDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // ==================================================
    // GET MY PROFILE
    // GET: api/Students/me
    // ==================================================

    [HttpGet("me")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var student =
            await _context.Students
                .Include(s => s.Department)
                .Include(s => s.StudentSkills)
                .ThenInclude(ss => ss.Skill)
                .FirstOrDefaultAsync(s =>
                    s.ApplicationUserId ==
                    userId);

        if (student == null)
        {
            return NotFound(new
            {
                message =
                    "Student profile not found."
            });
        }

        return Ok(new
        {
            profileExists = true,

            student.StudentId,

            student.FullName,

            student.Email,

            student.TenthPercentage,

            student.TwelfthPercentage,

            student.CGPA,

            student.Backlogs,

            student.GraduationYear,

            student.DepartmentId,

            department =
                student.Department?.Name,

            skills =
                student.StudentSkills
                    .Where(ss =>
                        ss.Skill != null)
                    .Select(ss =>
                        ss.Skill!.Name)
                    .OrderBy(name => name)
                    .ToList()
        });
    }

    // ==================================================
    // CREATE OR UPDATE MY PROFILE
    // PUT: api/Students/me
    // ==================================================

    [HttpPut("me")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult>
        SaveMyProfile(
            [FromBody]
            StudentProfileRequest request)
    {
        var userId =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var user =
            await _userManager
                .FindByIdAsync(userId);

        if (user == null)
        {
            return Unauthorized();
        }

        var validation =
            await ValidateAcademicProfile(
                request);

        if (validation != null)
        {
            return validation;
        }

        var existing =
            await _context.Students
                .FirstOrDefaultAsync(s =>
                    s.ApplicationUserId ==
                    userId);

        if (existing == null)
        {
            var email =
                user.Email?
                    .Trim()
                    .ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(
                email))
            {
                return BadRequest(new
                {
                    message =
                        "Logged-in account does not have a valid email."
                });
            }

            var duplicateEmail =
                await _context.Students
                    .AnyAsync(s =>
                        s.Email == email);

            if (duplicateEmail)
            {
                return BadRequest(new
                {
                    message =
                        "A student profile already exists with this email."
                });
            }

            var student =
                new Student
                {
                    FullName =
                        user.FullName.Trim(),

                    Email =
                        email,

                    TenthPercentage =
                        request.TenthPercentage,

                    TwelfthPercentage =
                        request.TwelfthPercentage,

                    CGPA =
                        request.CGPA,

                    Backlogs =
                        request.Backlogs,

                    GraduationYear =
                        request.GraduationYear,

                    DepartmentId =
                        request.DepartmentId,

                    ApplicationUserId =
                        userId
                };

            _context.Students.Add(
                student);

            await _context
                .SaveChangesAsync();

            return Ok(new
            {
                message =
                    "Student profile created successfully.",

                student.StudentId
            });
        }

        existing.TenthPercentage =
            request.TenthPercentage;

        existing.TwelfthPercentage =
            request.TwelfthPercentage;

        existing.CGPA =
            request.CGPA;

        existing.Backlogs =
            request.Backlogs;

        existing.GraduationYear =
            request.GraduationYear;

        existing.DepartmentId =
            request.DepartmentId;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message =
                "Student profile updated successfully.",

            existing.StudentId
        });
    }

    // ==================================================
    // GET ALL STUDENTS
    // ==================================================

    [HttpGet]
    [Authorize(
        Roles =
            "Admin,PlacementOfficer,Recruiter")]
    public async Task<
        ActionResult<IEnumerable<Student>>>
        GetStudents()
    {
        var students =
            await _context.Students
                .Include(s => s.Department)
                .Include(s => s.StudentSkills)
                .ThenInclude(ss => ss.Skill)
                .OrderBy(s => s.FullName)
                .ToListAsync();

        return Ok(students);
    }

    // ==================================================
    // GET STUDENT
    // ==================================================

    [HttpGet("{id:int}")]
    [Authorize(
        Roles =
            "Admin,PlacementOfficer,Recruiter,Student")]
    public async Task<IActionResult>
        GetStudent(int id)
    {
        var student =
            await _context.Students
                .Include(s => s.Department)
                .Include(s => s.StudentSkills)
                .ThenInclude(ss => ss.Skill)
                .FirstOrDefaultAsync(s =>
                    s.StudentId == id);

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

        return Ok(student);
    }

    // ==================================================
    // CREATE STUDENT
    // ADMIN / PLACEMENT OFFICER
    // ==================================================

    [HttpPost]
    [Authorize(
        Roles = "Admin,PlacementOfficer")]
    public async Task<IActionResult>
        CreateStudent(
            Student student)
    {
        if (string.IsNullOrWhiteSpace(
            student.FullName))
        {
            return BadRequest(new
            {
                message =
                    "Student name is required."
            });
        }

        if (string.IsNullOrWhiteSpace(
            student.Email))
        {
            return BadRequest(new
            {
                message =
                    "Student email is required."
            });
        }

        student.FullName =
            student.FullName.Trim();

        student.Email =
            student.Email
                .Trim()
                .ToLowerInvariant();

        var academicValidation =
            await ValidateAcademicProfile(
                new StudentProfileRequest
                {
                    TenthPercentage =
                        student.TenthPercentage,

                    TwelfthPercentage =
                        student.TwelfthPercentage,

                    CGPA =
                        student.CGPA,

                    Backlogs =
                        student.Backlogs,

                    GraduationYear =
                        student.GraduationYear,

                    DepartmentId =
                        student.DepartmentId
                });

        if (academicValidation != null)
        {
            return academicValidation;
        }

        var duplicate =
            await _context.Students
                .AnyAsync(s =>
                    s.Email ==
                    student.Email);

        if (duplicate)
        {
            return BadRequest(new
            {
                message =
                    "A student with this email already exists."
            });
        }

        // Admin-created student records do not
        // automatically own an Identity account.
        student.ApplicationUserId = null;

        _context.Students.Add(student);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetStudent),
            new
            {
                id = student.StudentId
            },
            student);
    }

    // ==================================================
    // UPDATE STUDENT
    // ==================================================

    [HttpPut("{id:int}")]
    [Authorize(
        Roles =
            "Admin,PlacementOfficer,Student")]
    public async Task<IActionResult>
        UpdateStudent(
            int id,
            Student student)
    {
        if (id != student.StudentId)
        {
            return BadRequest(new
            {
                message =
                    "Student ID in URL does not match StudentId in request body."
            });
        }

        var existing =
            await _context.Students
                .FirstOrDefaultAsync(s =>
                    s.StudentId == id);

        if (existing == null)
        {
            return NotFound(new
            {
                message =
                    "Student not found."
            });
        }

        if (User.IsInRole("Student") &&
            !OwnsStudent(existing))
        {
            return Forbid();
        }

        var validation =
            await ValidateAcademicProfile(
                new StudentProfileRequest
                {
                    TenthPercentage =
                        student.TenthPercentage,

                    TwelfthPercentage =
                        student.TwelfthPercentage,

                    CGPA =
                        student.CGPA,

                    Backlogs =
                        student.Backlogs,

                    GraduationYear =
                        student.GraduationYear,

                    DepartmentId =
                        student.DepartmentId
                });

        if (validation != null)
        {
            return validation;
        }

        // A student cannot change identity
        // name/email through this endpoint.
        if (!User.IsInRole("Student"))
        {
            if (string.IsNullOrWhiteSpace(
                student.FullName))
            {
                return BadRequest(new
                {
                    message =
                        "Student name is required."
                });
            }

            if (string.IsNullOrWhiteSpace(
                student.Email))
            {
                return BadRequest(new
                {
                    message =
                        "Student email is required."
                });
            }

            var normalizedEmail =
                student.Email
                    .Trim()
                    .ToLowerInvariant();

            var duplicate =
                await _context.Students
                    .AnyAsync(s =>
                        s.StudentId != id &&
                        s.Email ==
                        normalizedEmail);

            if (duplicate)
            {
                return BadRequest(new
                {
                    message =
                        "Another student already uses this email."
                });
            }

            existing.FullName =
                student.FullName.Trim();

            existing.Email =
                normalizedEmail;
        }

        existing.TenthPercentage =
            student.TenthPercentage;

        existing.TwelfthPercentage =
            student.TwelfthPercentage;

        existing.CGPA =
            student.CGPA;

        existing.Backlogs =
            student.Backlogs;

        existing.GraduationYear =
            student.GraduationYear;

        existing.DepartmentId =
            student.DepartmentId;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // ==================================================
    // DELETE STUDENT
    // ==================================================

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult>
        DeleteStudent(int id)
    {
        var student =
            await _context.Students
                .FindAsync(id);

        if (student == null)
        {
            return NotFound(new
            {
                message =
                    "Student not found."
            });
        }

        var hasApplications =
            await _context.Applications
                .AnyAsync(a =>
                    a.StudentId == id);

        if (hasApplications)
        {
            return BadRequest(new
            {
                message =
                    "Cannot delete student because applications are associated with this student."
            });
        }

        var hasPlacement =
            await _context.Placements
                .AnyAsync(p =>
                    p.StudentId == id);

        if (hasPlacement)
        {
            return BadRequest(new
            {
                message =
                    "Cannot delete student because a placement record exists."
            });
        }

        _context.Students.Remove(student);

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // ==================================================
    // VALIDATION
    // ==================================================

    private async Task<IActionResult?>
        ValidateAcademicProfile(
            StudentProfileRequest request)
    {
        if (request.TenthPercentage < 0 ||
            request.TenthPercentage > 100)
        {
            return BadRequest(new
            {
                message =
                    "10th percentage must be between 0 and 100."
            });
        }

        if (request.TwelfthPercentage < 0 ||
            request.TwelfthPercentage > 100)
        {
            return BadRequest(new
            {
                message =
                    "12th percentage must be between 0 and 100."
            });
        }

        if (request.CGPA < 0 ||
            request.CGPA > 10)
        {
            return BadRequest(new
            {
                message =
                    "CGPA must be between 0 and 10."
            });
        }

        if (request.Backlogs < 0)
        {
            return BadRequest(new
            {
                message =
                    "Backlogs cannot be negative."
            });
        }

        var currentYear =
            DateTime.UtcNow.Year;

        if (request.GraduationYear <
                currentYear ||
            request.GraduationYear >
                currentYear + 10)
        {
            return BadRequest(new
            {
                message =
                    $"Graduation year must be between {currentYear} and {currentYear + 10}."
            });
        }

        var departmentExists =
            await _context.Departments
                .AnyAsync(d =>
                    d.DepartmentId ==
                    request.DepartmentId);

        if (!departmentExists)
        {
            return BadRequest(new
            {
                message =
                    "Invalid DepartmentId. Department does not exist."
            });
        }

        return null;
    }

    private bool OwnsStudent(
        Student student)
    {
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

// ==================================================
// STUDENT PROFILE REQUEST
// ==================================================

public class StudentProfileRequest
{
    public decimal TenthPercentage
    { get; set; }

    public decimal TwelfthPercentage
    { get; set; }

    public decimal CGPA
    { get; set; }

    public int Backlogs
    { get; set; }

    public int GraduationYear
    { get; set; }

    public int DepartmentId
    { get; set; }
}