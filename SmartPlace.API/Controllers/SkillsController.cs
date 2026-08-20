using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartPlace.API.Data;
using SmartPlace.API.Models;

namespace SmartPlace.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SkillsController : ControllerBase
{
    private readonly SmartPlaceDbContext _context;

    public SkillsController(SmartPlaceDbContext context)
    {
        _context = context;
    }

    // GET: api/Skills
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Skill>>> GetSkills()
    {
        var skills = await _context.Skills
            .OrderBy(s => s.Name)
            .ToListAsync();

        return Ok(skills);
    }

    // GET: api/Skills/1
    [HttpGet("{id}")]
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

    // POST: api/Skills
    [HttpPost]
    public async Task<ActionResult<Skill>> CreateSkill(Skill skill)
    {
        if (string.IsNullOrWhiteSpace(skill.Name))
        {
            return BadRequest(new
            {
                message = "Skill name is required."
            });
        }

        var exists = await _context.Skills
            .AnyAsync(s => s.Name.ToLower() == skill.Name.ToLower());

        if (exists)
        {
            return BadRequest(new
            {
                message = "Skill already exists."
            });
        }

        skill.Name = skill.Name.Trim();

        _context.Skills.Add(skill);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetSkill),
            new { id = skill.SkillId },
            skill
        );
    }

    // PUT: api/Skills/1
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSkill(int id, Skill skill)
    {
        if (id != skill.SkillId)
        {
            return BadRequest(new
            {
                message = "Skill ID in URL does not match SkillId in request body."
            });
        }

        var existingSkill = await _context.Skills.FindAsync(id);

        if (existingSkill == null)
        {
            return NotFound(new
            {
                message = "Skill not found."
            });
        }

        if (string.IsNullOrWhiteSpace(skill.Name))
        {
            return BadRequest(new
            {
                message = "Skill name is required."
            });
        }

        var duplicateExists = await _context.Skills
            .AnyAsync(s =>
                s.SkillId != id &&
                s.Name.ToLower() == skill.Name.ToLower());

        if (duplicateExists)
        {
            return BadRequest(new
            {
                message = "Another skill with this name already exists."
            });
        }

        existingSkill.Name = skill.Name.Trim();

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/Skills/1
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSkill(int id)
    {
        var skill = await _context.Skills
            .Include(s => s.StudentSkills)
            .FirstOrDefaultAsync(s => s.SkillId == id);

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
                message = "Cannot delete skill because it is assigned to one or more students."
            });
        }

        _context.Skills.Remove(skill);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}