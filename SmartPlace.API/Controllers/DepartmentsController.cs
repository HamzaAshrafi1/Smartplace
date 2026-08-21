using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartPlace.API.Data;
using SmartPlace.API.Models;

namespace SmartPlace.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DepartmentsController : ControllerBase
{
    private readonly SmartPlaceDbContext _context;

    public DepartmentsController(SmartPlaceDbContext context)
    {
        _context = context;
    }

    // --------------------------------------------------
    // GET ALL DEPARTMENTS
    // All authenticated users
    // GET: api/Departments
    // --------------------------------------------------

    [HttpGet]
    [Authorize(Roles = "Admin,Student,Recruiter,PlacementOfficer")]
    public async Task<ActionResult<IEnumerable<Department>>> GetDepartments()
    {
        var departments = await _context.Departments
            .OrderBy(d => d.Name)
            .ToListAsync();

        return Ok(departments);
    }

    // --------------------------------------------------
    // GET DEPARTMENT BY ID
    // All authenticated users
    // GET: api/Departments/1
    // --------------------------------------------------

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,Student,Recruiter,PlacementOfficer")]
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

    // --------------------------------------------------
    // CREATE DEPARTMENT
    // Admin / Placement Officer
    // POST: api/Departments
    // --------------------------------------------------

    [HttpPost]
    [Authorize(Roles = "Admin,PlacementOfficer")]
    public async Task<ActionResult<Department>> CreateDepartment(
        Department department)
    {
        if (string.IsNullOrWhiteSpace(department.Name))
        {
            return BadRequest(new
            {
                message = "Department name is required."
            });
        }

        department.Name = department.Name.Trim();

        var exists = await _context.Departments
            .AnyAsync(d =>
                d.Name.ToLower() ==
                department.Name.ToLower());

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
            new
            {
                id = department.DepartmentId
            },
            department);
    }

    // --------------------------------------------------
    // UPDATE DEPARTMENT
    // Admin / Placement Officer
    // PUT: api/Departments/1
    // --------------------------------------------------

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,PlacementOfficer")]
    public async Task<IActionResult> UpdateDepartment(
        int id,
        Department department)
    {
        if (id != department.DepartmentId)
        {
            return BadRequest(new
            {
                message =
                    "Department ID in URL does not match DepartmentId in request body."
            });
        }

        if (string.IsNullOrWhiteSpace(department.Name))
        {
            return BadRequest(new
            {
                message = "Department name is required."
            });
        }

        var existingDepartment =
            await _context.Departments.FindAsync(id);

        if (existingDepartment == null)
        {
            return NotFound(new
            {
                message = "Department not found."
            });
        }

        department.Name = department.Name.Trim();

        var duplicateExists = await _context.Departments
            .AnyAsync(d =>
                d.DepartmentId != id &&
                d.Name.ToLower() ==
                department.Name.ToLower());

        if (duplicateExists)
        {
            return BadRequest(new
            {
                message =
                    "Another department with this name already exists."
            });
        }

        existingDepartment.Name = department.Name;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // --------------------------------------------------
    // DELETE DEPARTMENT
    // Admin only
    // DELETE: api/Departments/1
    // --------------------------------------------------

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteDepartment(int id)
    {
        var department = await _context.Departments
            .Include(d => d.Students)
            .FirstOrDefaultAsync(
                d => d.DepartmentId == id);

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
                message =
                    "Cannot delete department because students are assigned to it."
            });
        }

        _context.Departments.Remove(department);

        await _context.SaveChangesAsync();

        return NoContent();
    }
}