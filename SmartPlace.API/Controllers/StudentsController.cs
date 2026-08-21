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
public class StudentsController : ControllerBase
{
    private readonly SmartPlaceDbContext _context;

    public StudentsController(SmartPlaceDbContext context)
    {
        _context = context;
    }

    // --------------------------------------------------
    // GET ALL STUDENTS
    // Admin / Placement Officer / Recruiter
    // GET: api/Students
    // --------------------------------------------------

    [HttpGet]
    [Authorize(Roles = "Admin,PlacementOfficer,Recruiter")]
    public async Task<ActionResult<IEnumerable<Student>>> GetStudents()
    {
        var students = await _context.Students
            .Include(s => s.Department)
            .OrderBy(s => s.FullName)
            .ToListAsync();

        return Ok(students);
    }

    // --------------------------------------------------
    // GET LOGGED-IN STUDENT PROFILE
    // Student only
    // GET: api/Students/me
    // --------------------------------------------------

    [HttpGet("me")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new
            {
                message = "Unable to identify logged-in user."
            });
        }

        var student = await _context.Students
            .Include(s => s.Department)
            .Include(s => s.StudentSkills)
                .ThenInclude(ss => ss.Skill)
            .FirstOrDefaultAsync(
                s => s.ApplicationUserId == userId);

        if (student == null)
        {
            return NotFound(new
            {
                profileExists = false,
                message =
                    "Student profile has not been completed yet."
            });
        }

        return Ok(new
        {
            profileExists = true,

            student.StudentId,
            student.FullName,
            student.Email,
            student.CGPA,
            student.Backlogs,
            student.GraduationYear,
            student.DepartmentId,

            department =
                student.Department?.Name,

            skills = student.StudentSkills
                .Select(ss => ss.Skill.Name)
                .OrderBy(name => name)
                .ToList()
        });
    }

    // --------------------------------------------------
    // CREATE OR UPDATE LOGGED-IN STUDENT PROFILE
    // Student only
    // PUT: api/Students/me
    // --------------------------------------------------

    [HttpPut("me")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> UpdateMyProfile(
        Student student)
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        var userEmail = User.FindFirstValue(
            ClaimTypes.Email);

        var userName = User.FindFirstValue(
            ClaimTypes.Name);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new
            {
                message = "Unable to identify logged-in user."
            });
        }

        if (student.CGPA < 0 ||
            student.CGPA > 10)
        {
            return BadRequest(new
            {
                message =
                    "CGPA must be between 0 and 10."
            });
        }

        if (student.Backlogs < 0)
        {
            return BadRequest(new
            {
                message =
                    "Backlogs cannot be negative."
            });
        }

        if (student.GraduationYear <
                DateTime.UtcNow.Year - 10 ||
            student.GraduationYear >
                DateTime.UtcNow.Year + 10)
        {
            return BadRequest(new
            {
                message =
                    "Invalid graduation year."
            });
        }

        var departmentExists =
            await _context.Departments
                .AnyAsync(d =>
                    d.DepartmentId ==
                    student.DepartmentId);

        if (!departmentExists)
        {
            return BadRequest(new
            {
                message =
                    "Invalid DepartmentId. Department does not exist."
            });
        }

        var existingStudent =
            await _context.Students
                .FirstOrDefaultAsync(
                    s => s.ApplicationUserId ==
                         userId);

        // --------------------------------------------------
        // FIRST-TIME PROFILE CREATION
        // --------------------------------------------------

        if (existingStudent == null)
        {
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                return BadRequest(new
                {
                    message =
                        "Logged-in account does not contain an email address."
                });
            }

            var normalizedEmail =
                userEmail.Trim().ToLowerInvariant();

            var emailExists =
                await _context.Students
                    .AnyAsync(s =>
                        s.Email == normalizedEmail);

            if (emailExists)
            {
                return BadRequest(new
                {
                    message =
                        "A Student profile already exists with this email."
                });
            }

            var newStudent = new Student
            {
                FullName =
                    string.IsNullOrWhiteSpace(userName)
                        ? "Student"
                        : userName.Trim(),

                Email = normalizedEmail,

                CGPA = student.CGPA,

                Backlogs = student.Backlogs,

                GraduationYear =
                    student.GraduationYear,

                DepartmentId =
                    student.DepartmentId,

                ApplicationUserId =
                    userId
            };

            _context.Students.Add(newStudent);

            await _context.SaveChangesAsync();

            var createdStudent =
                await _context.Students
                    .Include(s => s.Department)
                    .FirstAsync(s =>
                        s.StudentId ==
                        newStudent.StudentId);

            return Ok(new
            {
                message =
                    "Student profile created successfully.",

                createdStudent.StudentId,
                createdStudent.FullName,
                createdStudent.Email,
                createdStudent.CGPA,
                createdStudent.Backlogs,
                createdStudent.GraduationYear,
                createdStudent.DepartmentId,

                department =
                    createdStudent.Department?.Name
            });
        }

        // --------------------------------------------------
        // EXISTING PROFILE UPDATE
        // --------------------------------------------------

        existingStudent.CGPA =
            student.CGPA;

        existingStudent.Backlogs =
            student.Backlogs;

        existingStudent.GraduationYear =
            student.GraduationYear;

        existingStudent.DepartmentId =
            student.DepartmentId;

        // FullName and Email come from Identity account,
        // not from arbitrary client input.
        if (!string.IsNullOrWhiteSpace(userName))
        {
            existingStudent.FullName =
                userName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(userEmail))
        {
            existingStudent.Email =
                userEmail.Trim()
                    .ToLowerInvariant();
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message =
                "Student profile updated successfully."
        });
    }

    // --------------------------------------------------
    // GET STUDENT BY ID
    // Admin / Placement Officer / Recruiter
    // Students must use /me
    // GET: api/Students/1
    // --------------------------------------------------

    [HttpGet("{id:int}")]
    [Authorize(
        Roles = "Admin,PlacementOfficer,Recruiter")]
    public async Task<ActionResult<Student>> GetStudent(
        int id)
    {
        var student = await _context.Students
            .Include(s => s.Department)
            .FirstOrDefaultAsync(
                s => s.StudentId == id);

        if (student == null)
        {
            return NotFound(new
            {
                message = "Student not found."
            });
        }

        return Ok(student);
    }

    // --------------------------------------------------
    // CREATE STUDENT
    // Admin / Placement Officer
    // POST: api/Students
    // --------------------------------------------------

    [HttpPost]
    [Authorize(Roles = "Admin,PlacementOfficer")]
    public async Task<ActionResult<Student>> CreateStudent(
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
            student.Email.Trim()
                .ToLowerInvariant();

        // Admin-created students are not automatically
        // linked to an Identity account.
        student.ApplicationUserId = null;

        var emailExists =
            await _context.Students
                .AnyAsync(s =>
                    s.Email == student.Email);

        if (emailExists)
        {
            return BadRequest(new
            {
                message =
                    "A student with this email already exists."
            });
        }

        var departmentExists =
            await _context.Departments
                .AnyAsync(d =>
                    d.DepartmentId ==
                    student.DepartmentId);

        if (!departmentExists)
        {
            return BadRequest(new
            {
                message =
                    "Invalid DepartmentId. Department does not exist."
            });
        }

        if (student.CGPA < 0 ||
            student.CGPA > 10)
        {
            return BadRequest(new
            {
                message =
                    "CGPA must be between 0 and 10."
            });
        }

        if (student.Backlogs < 0)
        {
            return BadRequest(new
            {
                message =
                    "Backlogs cannot be negative."
            });
        }

        if (student.GraduationYear <
                DateTime.UtcNow.Year - 10 ||
            student.GraduationYear >
                DateTime.UtcNow.Year + 10)
        {
            return BadRequest(new
            {
                message =
                    "Invalid graduation year."
            });
        }

        _context.Students.Add(student);

        await _context.SaveChangesAsync();

        var createdStudent =
            await _context.Students
                .Include(s => s.Department)
                .FirstAsync(s =>
                    s.StudentId ==
                    student.StudentId);

        return CreatedAtAction(
            nameof(GetStudent),
            new
            {
                id =
                    createdStudent.StudentId
            },
            createdStudent);
    }

    // --------------------------------------------------
    // UPDATE STUDENT BY ID
    // Admin / Placement Officer
    // Students must use PUT /me
    // PUT: api/Students/1
    // --------------------------------------------------

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,PlacementOfficer")]
    public async Task<IActionResult> UpdateStudent(
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

        var existingStudent =
            await _context.Students
                .FindAsync(id);

        if (existingStudent == null)
        {
            return NotFound(new
            {
                message = "Student not found."
            });
        }

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
            student.Email.Trim()
                .ToLowerInvariant();

        var duplicateEmail =
            await _context.Students
                .AnyAsync(s =>
                    s.StudentId != id &&
                    s.Email == student.Email);

        if (duplicateEmail)
        {
            return BadRequest(new
            {
                message =
                    "Another student already uses this email."
            });
        }

        var departmentExists =
            await _context.Departments
                .AnyAsync(d =>
                    d.DepartmentId ==
                    student.DepartmentId);

        if (!departmentExists)
        {
            return BadRequest(new
            {
                message =
                    "Invalid DepartmentId. Department does not exist."
            });
        }

        if (student.CGPA < 0 ||
            student.CGPA > 10)
        {
            return BadRequest(new
            {
                message =
                    "CGPA must be between 0 and 10."
            });
        }

        if (student.Backlogs < 0)
        {
            return BadRequest(new
            {
                message =
                    "Backlogs cannot be negative."
            });
        }

        existingStudent.FullName =
            student.FullName;

        existingStudent.Email =
            student.Email;

        existingStudent.CGPA =
            student.CGPA;

        existingStudent.Backlogs =
            student.Backlogs;

        existingStudent.GraduationYear =
            student.GraduationYear;

        existingStudent.DepartmentId =
            student.DepartmentId;

        // Never overwrite ApplicationUserId
        // through this endpoint.

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // --------------------------------------------------
    // DELETE STUDENT
    // Admin only
    // DELETE: api/Students/1
    // --------------------------------------------------

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteStudent(
        int id)
    {
        var student =
            await _context.Students
                .FindAsync(id);

        if (student == null)
        {
            return NotFound(new
            {
                message = "Student not found."
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
                    "Cannot delete student because a placement record is associated with this student."
            });
        }

        _context.Students.Remove(student);

        await _context.SaveChangesAsync();

        return NoContent();
    }
}