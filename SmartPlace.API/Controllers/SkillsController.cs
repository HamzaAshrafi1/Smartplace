using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartPlace.API.Data;
using SmartPlace.API.Models;

namespace SmartPlace.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SkillsController : ControllerBase
{
    private readonly SmartPlaceDbContext _context;

    public SkillsController(SmartPlaceDbContext context)
    {
        _context = context;
    }

    // --------------------------------------------------
    // GET ALL SKILLS
    // All authenticated users
    // GET: api/Skills
    // --------------------------------------------------

    [HttpGet]
    [Authorize(Roles = "Admin,Student,Recruiter,PlacementOfficer")]
    public async Task<ActionResult<IEnumerable<Skill>>> GetSkills()
    {
        var skills = await _context.Skills
            .OrderBy(s => s.Name)
            .ToListAsync();

        return Ok(skills);
    }

    // --------------------------------------------------
    // GET SKILL BY ID
    // All authenticated users
    // GET: api/Skills/1
    // --------------------------------------------------

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,Student,Recruiter,PlacementOfficer")]
    public async Task<ActionResult<Skill>> GetSkill(int id)
    {
        var skill = await _context.Skills
            .FirstOrDefaultAsync(s => s.SkillId == id);

        if (skill == null)
        {
            return NotFound(new
            {
                message = "Skill not found."
            });
        }

        return Ok(skill);
    }

    // --------------------------------------------------
    // CREATE SKILL
    // Admin / Placement Officer
    // POST: api/Skills
    // --------------------------------------------------

    [HttpPost]
    [Authorize(Roles = "Admin,PlacementOfficer")]
    public async Task<ActionResult<Skill>> CreateSkill(
        Skill skill)
    {
        if (string.IsNullOrWhiteSpace(skill.Name))
        {
            return BadRequest(new
            {
                message = "Skill name is required."
            });
        }

        skill.Name = skill.Name.Trim();

        var exists = await _context.Skills
            .AnyAsync(s =>
                s.Name.ToLower() ==
                skill.Name.ToLower());

        if (exists)
        {
            return BadRequest(new
            {
                message = "Skill already exists."
            });
        }

        _context.Skills.Add(skill);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetSkill),
            new
            {
                id = skill.SkillId
            },
            skill);
    }

    // --------------------------------------------------
    // UPDATE SKILL
    // Admin / Placement Officer
    // PUT: api/Skills/1
    // --------------------------------------------------

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,PlacementOfficer")]
    public async Task<IActionResult> UpdateSkill(
        int id,
        Skill skill)
    {
        if (id != skill.SkillId)
        {
            return BadRequest(new
            {
                message =
                    "Skill ID in URL does not match SkillId in request body."
            });
        }

        if (string.IsNullOrWhiteSpace(skill.Name))
        {
            return BadRequest(new
            {
                message = "Skill name is required."
            });
        }

        var existingSkill =
            await _context.Skills.FindAsync(id);

        if (existingSkill == null)
        {
            return NotFound(new
            {
                message = "Skill not found."
            });
        }

        skill.Name = skill.Name.Trim();

        var duplicateExists =
            await _context.Skills
                .AnyAsync(s =>
                    s.SkillId != id &&
                    s.Name.ToLower() ==
                    skill.Name.ToLower());

        if (duplicateExists)
        {
            return BadRequest(new
            {
                message =
                    "Another skill with this name already exists."
            });
        }

        existingSkill.Name = skill.Name;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // --------------------------------------------------
    // DELETE SKILL
    // Admin only
    // DELETE: api/Skills/1
    // --------------------------------------------------

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteSkill(int id)
    {
        var skill = await _context.Skills
            .Include(s => s.StudentSkills)
            .FirstOrDefaultAsync(
                s => s.SkillId == id);

        if (skill == null)
        {
            return NotFound(new
            {
                message = "Skill not found."
            });
        }

        if (skill.StudentSkills.Any())
        {
            return BadRequest(new
            {
                message =
                    "Cannot delete skill because it is assigned to one or more students."
            });
        }

        _context.Skills.Remove(skill);

        await _context.SaveChangesAsync();

        return NoContent();
    }
}