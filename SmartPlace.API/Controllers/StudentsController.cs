using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartPlace.API.Data;
using SmartPlace.API.Models;

namespace SmartPlace.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StudentsController : ControllerBase
{
    private readonly SmartPlaceDbContext _context;

    public StudentsController(SmartPlaceDbContext context)
    {
        _context = context;
    }

    // GET: api/Students
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Student>>> GetStudents()
    {
        var students = await _context.Students
            .Include(s => s.Department)
            .ToListAsync();

        return Ok(students);
    }

    // GET: api/Students/1
    [HttpGet("{id}")]
    public async Task<ActionResult<Student>> GetStudent(int id)
    {
        var student = await _context.Students
            .Include(s => s.Department)
            .FirstOrDefaultAsync(s => s.StudentId == id);

        if (student == null)
        {
            return NotFound(new
            {
                message = "Student not found."
            });
        }

        return Ok(student);
    }

    // POST: api/Students
    [HttpPost]
    public async Task<ActionResult<Student>> CreateStudent(Student student)
    {
        var departmentExists = await _context.Departments
            .AnyAsync(d => d.DepartmentId == student.DepartmentId);

        if (!departmentExists)
        {
            return BadRequest(new
            {
                message = "Invalid DepartmentId. Department does not exist."
            });
        }

        _context.Students.Add(student);
        await _context.SaveChangesAsync();

        var createdStudent = await _context.Students
            .Include(s => s.Department)
            .FirstAsync(s => s.StudentId == student.StudentId);

        return CreatedAtAction(
            nameof(GetStudent),
            new { id = createdStudent.StudentId },
            createdStudent
        );
    }

    // PUT: api/Students/1
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateStudent(int id, Student student)
    {
        if (id != student.StudentId)
        {
            return BadRequest(new
            {
                message = "Student ID in URL does not match StudentId in request body."
            });
        }

        var existingStudent = await _context.Students.FindAsync(id);

        if (existingStudent == null)
        {
            return NotFound(new
            {
                message = "Student not found."
            });
        }

        var departmentExists = await _context.Departments
            .AnyAsync(d => d.DepartmentId == student.DepartmentId);

        if (!departmentExists)
        {
            return BadRequest(new
            {
                message = "Invalid DepartmentId. Department does not exist."
            });
        }

        existingStudent.FullName = student.FullName;
        existingStudent.Email = student.Email;
        existingStudent.CGPA = student.CGPA;
        existingStudent.Backlogs = student.Backlogs;
        existingStudent.GraduationYear = student.GraduationYear;
        existingStudent.DepartmentId = student.DepartmentId;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/Students/1
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteStudent(int id)
    {
        var student = await _context.Students.FindAsync(id);

        if (student == null)
        {
            return NotFound(new
            {
                message = "Student not found."
            });
        }

        _context.Students.Remove(student);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}