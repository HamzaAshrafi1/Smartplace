using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartPlace.API.Data;
using SmartPlace.API.Models;
using SmartPlace.API.Services;

namespace SmartPlace.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AIController : ControllerBase
{
    private readonly SmartPlaceDbContext _context;

    private readonly SkillExtractionService
        _skillExtractionService;

    private readonly JobMatchingService
        _jobMatchingService;

    private readonly OpenAIAnalysisService
        _openAIAnalysisService;

    private readonly JobEligibilityService
        _eligibilityService;

    public AIController(
        SmartPlaceDbContext context,
        SkillExtractionService
            skillExtractionService,
        JobMatchingService
            jobMatchingService,
        OpenAIAnalysisService
            openAIAnalysisService,
        JobEligibilityService
            eligibilityService)
    {
        _context = context;

        _skillExtractionService =
            skillExtractionService;

        _jobMatchingService =
            jobMatchingService;

        _openAIAnalysisService =
            openAIAnalysisService;

        _eligibilityService =
            eligibilityService;
    }

    // ==================================================
    // EXTRACT SKILLS
    // ==================================================

    [HttpPost(
        "extract-skills/{studentId:int}")]
    [Authorize(
        Roles =
            "Student,Admin,PlacementOfficer")]
    public async Task<IActionResult>
        ExtractSkills(int studentId)
    {
        if (!await CanAccessStudentAsync(
                studentId))
        {
            return Forbid();
        }

        var student =
            await _context.Students
                .FirstOrDefaultAsync(s =>
                    s.StudentId ==
                    studentId);

        if (student == null)
        {
            return NotFound(new
            {
                message =
                    "Student not found."
            });
        }

        var resume =
            await _context.Resumes
                .FirstOrDefaultAsync(r =>
                    r.StudentId ==
                    studentId);

        if (resume == null)
        {
            return NotFound(new
            {
                message =
                    "Resume not found for this student."
            });
        }

        if (string.IsNullOrWhiteSpace(
            resume.ExtractedText))
        {
            return BadRequest(new
            {
                message =
                    "Resume text has not been extracted yet."
            });
        }

        var detectedSkills =
            _skillExtractionService
                .ExtractSkills(
                    resume.ExtractedText);

        if (detectedSkills.Count == 0)
        {
            resume.IsProcessed = true;

            await _context
                .SaveChangesAsync();

            return Ok(new
            {
                studentId,

                detectedSkills,

                newlyAddedSkills =
                    Array.Empty<string>(),

                totalSkillsDetected = 0,

                totalStudentSkills = 0,

                studentSkills =
                    Array.Empty<string>(),

                message =
                    "No recognizable skills were detected in the resume."
            });
        }

        var addedSkills =
            new List<string>();

        foreach (var skillName
                 in detectedSkills)
        {
            var normalized =
                skillName.Trim();

            var skill =
                await _context.Skills
                    .FirstOrDefaultAsync(s =>
                        s.Name.ToLower() ==
                        normalized.ToLower());

            if (skill == null)
            {
                skill =
                    new Skill
                    {
                        Name = normalized
                    };

                _context.Skills.Add(
                    skill);

                await _context
                    .SaveChangesAsync();
            }

            var assigned =
                await _context.StudentSkills
                    .AnyAsync(ss =>
                        ss.StudentId ==
                        studentId
                        &&
                        ss.SkillId ==
                        skill.SkillId);

            if (!assigned)
            {
                _context.StudentSkills.Add(
                    new StudentSkill
                    {
                        StudentId =
                            studentId,

                        SkillId =
                            skill.SkillId
                    });

                addedSkills.Add(
                    skill.Name);
            }
        }

        resume.IsProcessed = true;

        await _context.SaveChangesAsync();

        var studentSkills =
            await GetStudentSkills(
                studentId);

        return Ok(new
        {
            studentId,

            detectedSkills,

            newlyAddedSkills =
                addedSkills,

            totalSkillsDetected =
                detectedSkills.Count,

            totalStudentSkills =
                studentSkills.Count,

            studentSkills
        });
    }

    // ==================================================
    // SINGLE JOB MATCH
    // ==================================================

    [HttpGet(
        "job-match/{studentId:int}/{jobId:int}")]
    [Authorize(
        Roles =
            "Student,Recruiter,Admin,PlacementOfficer")]
    public async Task<IActionResult>
        GetJobMatch(
            int studentId,
            int jobId)
    {
        if (User.IsInRole("Student") &&
            !await CanAccessStudentAsync(
                studentId))
        {
            return Forbid();
        }

        var student =
            await _context.Students
                .Include(s => s.Department)
                .FirstOrDefaultAsync(s =>
                    s.StudentId ==
                    studentId);

        if (student == null)
        {
            return NotFound(new
            {
                message =
                    "Student not found."
            });
        }

        var job =
            await _context.Jobs
                .Include(j => j.Company)
                .Include(j =>
                    j.RequiredDepartment)
                .FirstOrDefaultAsync(j =>
                    j.JobId == jobId);

        if (job == null)
        {
            return NotFound(new
            {
                message =
                    "Job not found."
            });
        }

        if (User.IsInRole("Recruiter"))
        {
            var userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (job.Company?
                    .RecruiterUserId !=
                userId)
            {
                return Forbid();
            }
        }

        var eligibility =
            _eligibilityService
                .Evaluate(
                    student,
                    job);

        var studentSkills =
            await GetStudentSkills(
                studentId);

        if (studentSkills.Count == 0)
        {
            return BadRequest(new
            {
                message =
                    "No skills found for this student. Extract resume skills first."
            });
        }

        var jobText =
            $"{job.Title} {job.Description}";

        var requiredSkills =
            _skillExtractionService
                .ExtractSkills(jobText);

        var matchScore =
            requiredSkills.Count == 0
                ? 0
                : _jobMatchingService
                    .CalculateMatchScore(
                        studentSkills,
                        requiredSkills);

        var matchingSkills =
            requiredSkills.Count == 0
                ? new List<string>()
                : _jobMatchingService
                    .GetMatchingSkills(
                        studentSkills,
                        requiredSkills);

        var missingSkills =
            requiredSkills.Count == 0
                ? new List<string>()
                : _jobMatchingService
                    .GetMissingSkills(
                        studentSkills,
                        requiredSkills);

        string recommendation;

        if (!eligibility.IsEligible)
        {
            recommendation =
                "Not Academically Eligible";
        }
        else if (matchScore >= 70)
        {
            recommendation =
                "Strong Match";
        }
        else if (matchScore >= 40)
        {
            recommendation =
                "Moderate Match";
        }
        else
        {
            recommendation =
                "Low Skill Match";
        }

        string aiAnalysis;

        try
        {
            aiAnalysis =
                await _openAIAnalysisService
                    .GenerateJobMatchExplanation(
                        student.FullName,
                        job.Title,
                        matchScore,
                        matchingSkills,
                        missingSkills,
                        eligibility
                            .IsEligible);
        }
        catch
        {
            aiAnalysis =
                "AI explanation is currently unavailable. " +
                "The eligibility and skill-match results remain valid.";
        }

        return Ok(new
        {
            student = new
            {
                student.StudentId,

                student.FullName,

                student.TenthPercentage,

                student.TwelfthPercentage,

                student.CGPA,

                student.Backlogs,

                student.GraduationYear,

                department =
                    student.Department?.Name
            },

            job = new
            {
                job.JobId,

                job.Title,

                company =
                    job.Company?.Name,

                job.Package,

                requiredDepartment =
                    job.RequiredDepartment?.Name,

                job.MinimumTenthPercentage,

                job.MinimumTwelfthPercentage,

                job.MinimumCGPA,

                job.MaximumBacklogs,

                job.GraduationYear
            },

            skillAnalysis = new
            {
                matchPercentage =
                    matchScore,

                studentSkills,

                requiredSkills,

                matchingSkills,

                missingSkills
            },

            eligibility = new
            {
                academicallyEligible =
                    eligibility.IsEligible,

                eligibility
                    .DepartmentEligible,

                eligibility
                    .TenthEligible,

                eligibility
                    .TwelfthEligible,

                eligibility
                    .CGPAEligible,

                eligibility
                    .BacklogEligible,

                eligibility
                    .GraduationYearEligible,

                eligibility.Reasons
            },

            recommendation,

            aiAnalysis
        });
    }

    // ==================================================
    // AI RECOMMENDATIONS
    //
    // ONLY academically eligible jobs enter
    // the AI recommendation ranking.
    // ==================================================

    [HttpGet(
        "recommend-jobs/{studentId:int}")]
    [Authorize(
        Roles =
            "Student,Admin,PlacementOfficer")]
    public async Task<IActionResult>
        RecommendJobs(int studentId)
    {
        if (!await CanAccessStudentAsync(
                studentId))
        {
            return Forbid();
        }

        var student =
            await _context.Students
                .Include(s => s.Department)
                .FirstOrDefaultAsync(s =>
                    s.StudentId ==
                    studentId);

        if (student == null)
        {
            return NotFound(new
            {
                message =
                    "Student not found."
            });
        }

        var studentSkills =
            await GetStudentSkills(
                studentId);

        if (studentSkills.Count == 0)
        {
            return BadRequest(new
            {
                message =
                    "No skills found for this student. Extract resume skills first."
            });
        }

        var jobs =
            await _context.Jobs
                .Include(j => j.Company)
                .Include(j =>
                    j.RequiredDepartment)
                .Where(j =>
                    j.Status == "Published")
                .ToListAsync();

        if (jobs.Count == 0)
        {
            return NotFound(new
            {
                message =
                    "No published jobs are currently available."
            });
        }

        var recommendations =
            new List<JobRecommendation>();

        foreach (var job in jobs)
        {
            var eligibility =
                _eligibilityService
                    .Evaluate(
                        student,
                        job);

            // ==================================================
            // CRITICAL:
            // Ineligible jobs are NOT AI recommendations.
            // ==================================================

            if (!eligibility.IsEligible)
            {
                continue;
            }

            var jobText =
                $"{job.Title} {job.Description}";

            var requiredSkills =
                _skillExtractionService
                    .ExtractSkills(jobText);

            double matchScore = 0;

            var matchingSkills =
                new List<string>();

            var missingSkills =
                new List<string>();

            if (requiredSkills.Count > 0)
            {
                matchScore =
                    _jobMatchingService
                        .CalculateMatchScore(
                            studentSkills,
                            requiredSkills);

                matchingSkills =
                    _jobMatchingService
                        .GetMatchingSkills(
                            studentSkills,
                            requiredSkills);

                missingSkills =
                    _jobMatchingService
                        .GetMissingSkills(
                            studentSkills,
                            requiredSkills);
            }

            string recommendation;

            if (matchScore >= 70)
            {
                recommendation =
                    "Strong Match";
            }
            else if (matchScore >= 40)
            {
                recommendation =
                    "Moderate Match";
            }
            else
            {
                recommendation =
                    "Low Skill Match";
            }

            recommendations.Add(
                new JobRecommendation
                {
                    JobId =
                        job.JobId,

                    JobTitle =
                        job.Title,

                    Company =
                        job.Company?.Name,

                    Package =
                        job.Package,

                    Location =
                        job.Location,

                    MatchPercentage =
                        matchScore,

                    AcademicallyEligible =
                        true,

                    MatchingSkills =
                        matchingSkills,

                    MissingSkills =
                        missingSkills,

                    Recommendation =
                        recommendation
                });
        }

        var ranked =
            recommendations
                .OrderByDescending(r =>
                    r.MatchPercentage)
                .ThenBy(r =>
                    r.JobTitle)
                .ToList();

        return Ok(new
        {
            student = new
            {
                student.StudentId,

                student.FullName,

                student.TenthPercentage,

                student.TwelfthPercentage,

                student.CGPA,

                student.Backlogs,

                student.GraduationYear,

                department =
                    student.Department?.Name
            },

            studentSkills,

            totalPublishedJobs =
                jobs.Count,

            totalEligibleJobs =
                ranked.Count,

            totalJobsAnalyzed =
                ranked.Count,

            recommendations =
                ranked
        });
    }

    // ==================================================
    // STUDENT SKILLS
    // ==================================================

    private async Task<List<string>>
        GetStudentSkills(int studentId)
    {
        return await _context
            .StudentSkills
            .Where(ss =>
                ss.StudentId ==
                studentId)
            .Include(ss => ss.Skill)
            .Where(ss =>
                ss.Skill != null)
            .Select(ss =>
                ss.Skill!.Name)
            .OrderBy(name => name)
            .ToListAsync();
    }

    // ==================================================
    // OWNERSHIP
    // ==================================================

    private async Task<bool>
        CanAccessStudentAsync(
            int studentId)
    {
        // Admin and PlacementOfficer can
        // intentionally work with students.
        if (!User.IsInRole("Student"))
        {
            return true;
        }

        var userId =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(
            userId))
        {
            return false;
        }

        return await _context.Students
            .AnyAsync(s =>
                s.StudentId ==
                studentId
                &&
                s.ApplicationUserId ==
                userId);
    }

    // ==================================================
    // INTERNAL RECOMMENDATION MODEL
    // ==================================================

    private sealed class JobRecommendation
    {
        public int JobId { get; set; }

        public string JobTitle { get; set; } =
            string.Empty;

        public string? Company { get; set; }

        public decimal Package { get; set; }

        public string? Location { get; set; }

        public double MatchPercentage
        { get; set; }

        public bool AcademicallyEligible
        { get; set; }

        public List<string> MatchingSkills
        { get; set; } = new();

        public List<string> MissingSkills
        { get; set; } = new();

        public string Recommendation
        { get; set; } = string.Empty;
    }
}