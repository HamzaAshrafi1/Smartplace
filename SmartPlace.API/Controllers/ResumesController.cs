using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UglyToad.PdfPig;
using SmartPlace.API.Data;
using SmartPlace.API.Models;
using UglyToad.PdfPig;

namespace SmartPlace.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ResumesController : ControllerBase
{
    private readonly SmartPlaceDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public ResumesController(
        SmartPlaceDbContext context,
        IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    // GET: api/Resumes
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Resume>>> GetResumes()
    {
        var resumes = await _context.Resumes
            .Include(r => r.Student)
            .OrderByDescending(r => r.UploadedAt)
            .ToListAsync();

        return Ok(resumes);
    }

    // GET: api/Resumes/1
    [HttpGet("{id}")]
    public async Task<ActionResult<Resume>> GetResume(int id)
    {
        var resume = await _context.Resumes
            .Include(r => r.Student)
            .FirstOrDefaultAsync(r => r.ResumeId == id);

        if (resume == null)
        {
            return NotFound(new
            {
                message = "Resume not found."
            });
        }

        return Ok(resume);
    }

    // POST: api/Resumes/upload/1
    [HttpPost("upload/{studentId}")]
    public async Task<IActionResult> UploadResume(
        int studentId,
        IFormFile file)
    {
        var student = await _context.Students
            .FindAsync(studentId);

        if (student == null)
        {
            return BadRequest(new
            {
                message = "Student not found."
            });
        }

        if (file == null || file.Length == 0)
        {
            return BadRequest(new
            {
                message = "No file selected."
            });
        }

        if (!file.FileName.EndsWith(".pdf",
            StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                message = "Only PDF files are allowed."
            });
        }

        var uploadFolder = Path.Combine(
            _environment.ContentRootPath,
            "Uploads",
            "Resumes");

        Directory.CreateDirectory(uploadFolder);

        var savedFileName =
            $"{Guid.NewGuid()}_{file.FileName}";

        var savedFilePath = Path.Combine(
            uploadFolder,
            savedFileName);

        using (var stream =
               new FileStream(
                   savedFilePath,
                   FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        string extractedText = "";

        try
        {
            using var document =
                PdfDocument.Open(savedFilePath);

            foreach (var page in document.GetPages())
            {
                extractedText += page.Text + Environment.NewLine;
            }
        }
        catch
        {
            return BadRequest(new
            {
                message = "Unable to extract text from the PDF."
            });
        }

        var existingResume =
            await _context.Resumes
                .FirstOrDefaultAsync(
                    r => r.StudentId == studentId);

        if (existingResume != null)
        {
            existingResume.FileName = file.FileName;
            existingResume.FilePath = savedFilePath;
            existingResume.ExtractedText = extractedText;
            existingResume.UploadedAt = DateTime.UtcNow;
            existingResume.IsProcessed = true;

            await _context.SaveChangesAsync();

            return Ok(existingResume);
        }

        var resume = new Resume
        {
            StudentId = studentId,
            FileName = file.FileName,
            FilePath = savedFilePath,
            ExtractedText = extractedText,
            UploadedAt = DateTime.UtcNow,
            IsProcessed = true
        };

        _context.Resumes.Add(resume);

        await _context.SaveChangesAsync();

        return Ok(resume);
    }

    // DELETE: api/Resumes/1
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteResume(int id)
    {
        var resume =
            await _context.Resumes.FindAsync(id);

        if (resume == null)
        {
            return NotFound(new
            {
                message = "Resume not found."
            });
        }

        if (System.IO.File.Exists(resume.FilePath))
        {
            System.IO.File.Delete(resume.FilePath);
        }

        _context.Resumes.Remove(resume);

        await _context.SaveChangesAsync();

        return NoContent();
    }
}