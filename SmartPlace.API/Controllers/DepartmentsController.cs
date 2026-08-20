using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartPlace.API.Data;
using SmartPlace.API.Models;

namespace SmartPlace.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DepartmentsController : ControllerBase
{
    private readonly SmartPlaceDbContext _context;

    public DepartmentsController(SmartPlaceDbContext context)
    {
        _context = context;
    }

    // GET: api/Departments
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Department>>> GetDepartments()
    {
        var departments = await _context.Departments
            .OrderBy(d => d.Name)
            .ToListAsync();

        return Ok(departments);
    }

    // GET: api/Departments/1
    [HttpGet("{id}")]
    public async Task<ActionResult<Department>> GetDepartment(int id)
    {
        var department = await _context.Departments
            .FirstOrDefaultAsync(d => d.DepartmentId == id);

        if (department == null)
        {
            return NotFound(new
            {
                message = "Department not found."
            });
        }

        return Ok(department);
    }

    // POST: api/Departments
    [HttpPost]
    public async Task<ActionResult<Department>> CreateDepartment(Department department)
    {
        var exists = await _context.Departments
            .AnyAsync(d => d.Name.ToLower() == department.Name.ToLower());

        if (exists)
        {
            return BadRequest(new
            {
                message = "Department already exists."
            });
        }

        _context.Departments.Add(department);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetDepartment),
            new { id = department.DepartmentId },
            department
        );
    }

    // PUT: api/Departments/1
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateDepartment(int id, Department department)
    {
        if (id != department.DepartmentId)
        {
            return BadRequest(new
            {
                message = "Department ID in URL does not match DepartmentId in request body."
            });
        }

        var existingDepartment = await _context.Departments.FindAsync(id);

        if (existingDepartment == null)
        {
            return NotFound(new
            {
                message = "Department not found."
            });
        }

        var duplicateExists = await _context.Departments
            .AnyAsync(d =>
                d.DepartmentId != id &&
                d.Name.ToLower() == department.Name.ToLower());

        if (duplicateExists)
        {
            return BadRequest(new
            {
                message = "Another department with this name already exists."
            });
        }

        existingDepartment.Name = department.Name;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/Departments/1
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDepartment(int id)
    {
        var department = await _context.Departments
            .Include(d => d.Students)
            .FirstOrDefaultAsync(d => d.DepartmentId == id);

        if (department == null)
        {
            return NotFound(new
            {
                message = "Department not found."
            });
        }

        if (department.Students.Any())
        {
            return BadRequest(new
            {
                message = "Cannot delete department because students are assigned to it."
            });
        }

        _context.Departments.Remove(department);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}