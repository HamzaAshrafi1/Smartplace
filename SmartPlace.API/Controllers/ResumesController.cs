using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UglyToad.PdfPig;
using SmartPlace.API.Data;
using SmartPlace.API.Models;

namespace SmartPlace.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ResumesController : ControllerBase
{
    private readonly SmartPlaceDbContext _context;
    private readonly IWebHostEnvironment _environment;

    private const long MaxFileSize = 5 * 1024 * 1024;

    public ResumesController(
        SmartPlaceDbContext context,
        IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    // --------------------------------------------------
    // GET ALL RESUMES
    // Admin / Recruiter / Placement Officer
    // GET: api/Resumes
    // --------------------------------------------------

    [HttpGet]
    [Authorize(Roles = "Admin,Recruiter,PlacementOfficer")]
    public async Task<ActionResult<IEnumerable<Resume>>> GetResumes()
    {
        var resumes = await _context.Resumes
            .Include(r => r.Student)
            .OrderByDescending(r => r.UploadedAt)
            .ToListAsync();

        return Ok(resumes);
    }

    // --------------------------------------------------
    // GET LOGGED-IN STUDENT RESUME
    // Student only
    // GET: api/Resumes/me
    // --------------------------------------------------

    [HttpGet("me")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetMyResume()
    {
        var userId =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new
            {
                message =
                    "Unable to identify logged-in user."
            });
        }

        var student = await _context.Students
            .FirstOrDefaultAsync(
                s => s.ApplicationUserId == userId);

        if (student == null)
        {
            return NotFound(new
            {
                message =
                    "Student profile not found."
            });
        }

        var resume = await _context.Resumes
            .FirstOrDefaultAsync(
                r => r.StudentId == student.StudentId);

        if (resume == null)
        {
            return NotFound(new
            {
                message =
                    "Resume not found."
            });
        }

        return Ok(resume);
    }

    // --------------------------------------------------
    // GET RESUME BY ID
    // Admin / Recruiter / Placement Officer
    // GET: api/Resumes/1
    // --------------------------------------------------

    [HttpGet("{id:int}")]
    [Authorize(Roles = "Admin,Recruiter,PlacementOfficer")]
    public async Task<ActionResult<Resume>> GetResume(int id)
    {
        var resume = await _context.Resumes
            .Include(r => r.Student)
            .FirstOrDefaultAsync(
                r => r.ResumeId == id);

        if (resume == null)
        {
            return NotFound(new
            {
                message = "Resume not found."
            });
        }

        return Ok(resume);
    }

    // --------------------------------------------------
    // UPLOAD / REPLACE RESUME
    // Student / Admin / Placement Officer
    // POST: api/Resumes/upload/1
    // --------------------------------------------------

    [HttpPost("upload/{studentId}")]
    [Authorize(Roles = "Student,Admin,PlacementOfficer")]
    [RequestSizeLimit(MaxFileSize)]
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

        // Student can upload only their own resume
        if (User.IsInRole("Student"))
        {
            var userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId) ||
                student.ApplicationUserId != userId)
            {
                return Forbid();
            }
        }

        if (file == null || file.Length == 0)
        {
            return BadRequest(new
            {
                message = "No file selected."
            });
        }

        if (file.Length > MaxFileSize)
        {
            return BadRequest(new
            {
                message =
                    "Resume PDF cannot exceed 5 MB."
            });
        }

        var extension =
            Path.GetExtension(file.FileName);

        if (!string.Equals(
                extension,
                ".pdf",
                StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                message =
                    "Only PDF files are allowed."
            });
        }

        var allowedContentTypes = new[]
        {
            "application/pdf",
            "application/octet-stream"
        };

        if (!allowedContentTypes.Contains(
                file.ContentType,
                StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                message =
                    "Invalid PDF content type."
            });
        }

        var uploadFolder = Path.Combine(
            _environment.ContentRootPath,
            "Uploads",
            "Resumes");

        Directory.CreateDirectory(
            uploadFolder);

        var savedFileName =
            $"{Guid.NewGuid():N}.pdf";

        var savedFilePath =
            Path.Combine(
                uploadFolder,
                savedFileName);

        try
        {
            await using (var stream =
                new FileStream(
                    savedFilePath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None))
            {
                await file.CopyToAsync(stream);
            }

            var extractedText =
                new System.Text.StringBuilder();

            using (var document =
                   PdfDocument.Open(savedFilePath))
            {
                foreach (var page in document.GetPages())
                {
                    extractedText.AppendLine(
                        page.Text);
                }
            }

            var extractedTextValue =
                extractedText
                    .ToString()
                    .Trim();

            if (string.IsNullOrWhiteSpace(
                extractedTextValue))
            {
                if (System.IO.File.Exists(
                    savedFilePath))
                {
                    System.IO.File.Delete(
                        savedFilePath);
                }

                return BadRequest(new
                {
                    message =
                        "No readable text could be extracted from the PDF."
                });
            }

            var existingResume =
                await _context.Resumes
                    .FirstOrDefaultAsync(
                        r =>
                            r.StudentId ==
                            studentId);

            if (existingResume != null)
            {
                var oldFilePath =
                    existingResume.FilePath;

                existingResume.FileName =
                    Path.GetFileName(
                        file.FileName);

                existingResume.FilePath =
                    savedFilePath;

                existingResume.ExtractedText =
                    extractedTextValue;

                existingResume.UploadedAt =
                    DateTime.UtcNow;

                existingResume.IsProcessed =
                    true;

                await _context
                    .SaveChangesAsync();

                if (!string.IsNullOrWhiteSpace(
                        oldFilePath) &&
                    !string.Equals(
                        oldFilePath,
                        savedFilePath,
                        StringComparison
                            .OrdinalIgnoreCase) &&
                    System.IO.File.Exists(
                        oldFilePath))
                {
                    System.IO.File.Delete(
                        oldFilePath);
                }

                return Ok(existingResume);
            }

            var resume = new Resume
            {
                StudentId = studentId,

                FileName =
                    Path.GetFileName(
                        file.FileName),

                FilePath =
                    savedFilePath,

                ExtractedText =
                    extractedTextValue,

                UploadedAt =
                    DateTime.UtcNow,

                IsProcessed =
                    true
            };

            _context.Resumes.Add(resume);

            await _context.SaveChangesAsync();

            return Ok(resume);
        }
        catch
        {
            if (System.IO.File.Exists(
                savedFilePath))
            {
                System.IO.File.Delete(
                    savedFilePath);
            }

            return BadRequest(new
            {
                message =
                    "Unable to process the uploaded PDF."
            });
        }
    }

    // --------------------------------------------------
    // DELETE RESUME
    // Student / Admin
    // DELETE: api/Resumes/1
    // --------------------------------------------------

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Student,Admin")]
    public async Task<IActionResult> DeleteResume(
        int id)
    {
        var resume =
            await _context.Resumes
                .Include(r => r.Student)
                .FirstOrDefaultAsync(
                    r => r.ResumeId == id);

        if (resume == null)
        {
            return NotFound(new
            {
                message = "Resume not found."
            });
        }

        // Student can delete only their own resume
        if (User.IsInRole("Student"))
        {
            var userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (resume.Student == null ||
                resume.Student.ApplicationUserId !=
                userId)
            {
                return Forbid();
            }
        }

        var filePath =
            resume.FilePath;

        _context.Resumes.Remove(resume);

        await _context.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(
                filePath) &&
            System.IO.File.Exists(filePath))
        {
            System.IO.File.Delete(filePath);
        }

        return NoContent();
    }
}