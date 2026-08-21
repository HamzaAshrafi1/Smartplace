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
    private readonly SkillExtractionService _skillExtractionService;
    private readonly JobMatchingService _jobMatchingService;
    private readonly OpenAIAnalysisService _openAIAnalysisService;

    public AIController(
        SmartPlaceDbContext context,
        SkillExtractionService skillExtractionService,
        JobMatchingService jobMatchingService,
        OpenAIAnalysisService openAIAnalysisService)
    {
        _context = context;
        _skillExtractionService = skillExtractionService;
        _jobMatchingService = jobMatchingService;
        _openAIAnalysisService = openAIAnalysisService;
    }

    // --------------------------------------------------
    // EXTRACT AND SAVE SKILLS FROM RESUME
    // Student / Admin / Placement Officer
    // POST: api/AI/extract-skills/1
    // --------------------------------------------------

    [HttpPost("extract-skills/{studentId}")]
    [Authorize(Roles = "Student,Admin,PlacementOfficer")]
    public async Task<IActionResult> ExtractSkills(int studentId)
    {
        var student = await _context.Students
            .FirstOrDefaultAsync(
                s => s.StudentId == studentId);

        if (student == null)
        {
            return NotFound(new
            {
                message = "Student not found."
            });
        }

        var resume = await _context.Resumes
            .FirstOrDefaultAsync(
                r => r.StudentId == studentId);

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
            _skillExtractionService.ExtractSkills(
                resume.ExtractedText);

        if (detectedSkills.Count == 0)
        {
            resume.IsProcessed = true;

            await _context.SaveChangesAsync();

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

        var addedSkills = new List<string>();

        foreach (var skillName in detectedSkills)
        {
            var normalizedSkillName =
                skillName.Trim();

            var skill = await _context.Skills
                .FirstOrDefaultAsync(s =>
                    s.Name.ToLower() ==
                    normalizedSkillName.ToLower());

            if (skill == null)
            {
                skill = new Skill
                {
                    Name = normalizedSkillName
                };

                _context.Skills.Add(skill);

                await _context.SaveChangesAsync();
            }

            var alreadyAssigned =
                await _context.StudentSkills
                    .AnyAsync(ss =>
                        ss.StudentId == studentId &&
                        ss.SkillId == skill.SkillId);

            if (!alreadyAssigned)
            {
                var studentSkill =
                    new StudentSkill
                    {
                        StudentId = studentId,
                        SkillId = skill.SkillId
                    };

                _context.StudentSkills.Add(
                    studentSkill);

                addedSkills.Add(skill.Name);
            }
        }

        resume.IsProcessed = true;

        await _context.SaveChangesAsync();

        var studentSkills =
            await GetStudentSkills(studentId);

        return Ok(new
        {
            studentId,

            detectedSkills,

            newlyAddedSkills = addedSkills,

            totalSkillsDetected =
                detectedSkills.Count,

            totalStudentSkills =
                studentSkills.Count,

            studentSkills
        });
    }

    // --------------------------------------------------
    // HYBRID AI JOB MATCH
    // Student / Recruiter / Admin / Placement Officer
    // GET: api/AI/job-match/1/1
    // --------------------------------------------------

    [HttpGet("job-match/{studentId}/{jobId}")]
    [Authorize(
        Roles = "Student,Recruiter,Admin,PlacementOfficer")]
    public async Task<IActionResult> GetJobMatch(
        int studentId,
        int jobId)
    {
        var student = await _context.Students
            .FirstOrDefaultAsync(
                s => s.StudentId == studentId);

        if (student == null)
        {
            return NotFound(new
            {
                message = "Student not found."
            });
        }

        var job = await _context.Jobs
            .Include(j => j.Company)
            .FirstOrDefaultAsync(
                j => j.JobId == jobId);

        if (job == null)
        {
            return NotFound(new
            {
                message = "Job not found."
            });
        }

        var studentSkills =
            await GetStudentSkills(studentId);

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

        if (requiredSkills.Count == 0)
        {
            return BadRequest(new
            {
                message =
                    "No recognizable required skills were found in the job description."
            });
        }

        var matchScore =
            _jobMatchingService.CalculateMatchScore(
                studentSkills,
                requiredSkills);

        var matchingSkills =
            _jobMatchingService.GetMatchingSkills(
                studentSkills,
                requiredSkills);

        var missingSkills =
            _jobMatchingService.GetMissingSkills(
                studentSkills,
                requiredSkills);

        var cgpaEligible =
            student.CGPA >= job.MinimumCGPA;

        var backlogEligible =
            student.Backlogs <=
            job.MaximumBacklogs;

        var graduationYearEligible =
            student.GraduationYear ==
            job.GraduationYear;

        var academicallyEligible =
            cgpaEligible &&
            backlogEligible &&
            graduationYearEligible;

        string recommendation;

        if (!academicallyEligible)
        {
            recommendation =
                "Not Academically Eligible";
        }
        else if (matchScore >= 70)
        {
            recommendation = "Strong Match";
        }
        else if (matchScore >= 40)
        {
            recommendation = "Moderate Match";
        }
        else
        {
            recommendation = "Low Skill Match";
        }

        // OpenAI is used only for the explanation.
        // Core matching still works if OpenAI fails.
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
                        academicallyEligible);
        }
        catch
        {
            aiAnalysis =
                "AI explanation is currently unavailable. " +
                "The calculated eligibility and skill-match results are still valid.";
        }

        return Ok(new
        {
            student = new
            {
                student.StudentId,
                student.FullName,
                student.CGPA,
                student.Backlogs,
                student.GraduationYear
            },

            job = new
            {
                job.JobId,
                job.Title,
                company = job.Company?.Name,
                job.Package,
                job.MinimumCGPA,
                job.MaximumBacklogs,
                job.GraduationYear
            },

            skillAnalysis = new
            {
                matchPercentage = matchScore,
                studentSkills,
                requiredSkills,
                matchingSkills,
                missingSkills
            },

            eligibility = new
            {
                cgpaEligible,
                backlogEligible,
                graduationYearEligible,
                academicallyEligible
            },

            recommendation,

            aiAnalysis
        });
    }

    // --------------------------------------------------
    // AI JOB RECOMMENDATIONS
    // Student / Admin / Placement Officer
    // GET: api/AI/recommend-jobs/1
    // --------------------------------------------------

    [HttpGet("recommend-jobs/{studentId}")]
    [Authorize(Roles = "Student,Admin,PlacementOfficer")]
    public async Task<IActionResult> RecommendJobs(
        int studentId)
    {
        var student = await _context.Students
            .FirstOrDefaultAsync(
                s => s.StudentId == studentId);

        if (student == null)
        {
            return NotFound(new
            {
                message = "Student not found."
            });
        }

        var studentSkills =
            await GetStudentSkills(studentId);

        if (studentSkills.Count == 0)
        {
            return BadRequest(new
            {
                message =
                    "No skills found for this student. Extract resume skills first."
            });
        }

        var jobs = await _context.Jobs
            .Include(j => j.Company)
            .Where(j =>
                j.Status.ToLower() ==
                "published")
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
            var jobText =
                $"{job.Title} {job.Description}";

            var requiredSkills =
                _skillExtractionService
                    .ExtractSkills(jobText);

            var matchScore = 0.0;

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

            var cgpaEligible =
                student.CGPA >=
                job.MinimumCGPA;

            var backlogEligible =
                student.Backlogs <=
                job.MaximumBacklogs;

            var graduationYearEligible =
                student.GraduationYear ==
                job.GraduationYear;

            var academicallyEligible =
                cgpaEligible &&
                backlogEligible &&
                graduationYearEligible;

            string recommendation;

            if (!academicallyEligible)
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

            recommendations.Add(
                new JobRecommendation
                {
                    JobId = job.JobId,
                    JobTitle = job.Title,
                    Company =
                        job.Company?.Name,
                    Package = job.Package,
                    Location = job.Location,
                    MatchPercentage =
                        matchScore,
                    AcademicallyEligible =
                        academicallyEligible,
                    MatchingSkills =
                        matchingSkills,
                    MissingSkills =
                        missingSkills,
                    Recommendation =
                        recommendation
                });
        }

        var rankedRecommendations =
            recommendations
                .OrderByDescending(r =>
                    r.AcademicallyEligible)
                .ThenByDescending(r =>
                    r.MatchPercentage)
                .ToList();

        return Ok(new
        {
            student = new
            {
                student.StudentId,
                student.FullName,
                student.CGPA,
                student.Backlogs,
                student.GraduationYear
            },

            studentSkills,

            totalJobsAnalyzed =
                rankedRecommendations.Count,

            recommendations =
                rankedRecommendations
        });
    }

    // --------------------------------------------------
    // PRIVATE HELPER
    // --------------------------------------------------

    private async Task<List<string>>
        GetStudentSkills(int studentId)
    {
        return await _context.StudentSkills
            .Where(ss =>
                ss.StudentId == studentId)
            .Include(ss => ss.Skill)
            .Select(ss => ss.Skill.Name)
            .OrderBy(name => name)
            .ToListAsync();
    }

    // --------------------------------------------------
    // PRIVATE RESULT MODEL
    // Avoids reflection when ranking recommendations
    // --------------------------------------------------

    private sealed class JobRecommendation
    {
        public int JobId { get; set; }

        public string JobTitle { get; set; } =
            string.Empty;

        public string? Company { get; set; }

        public decimal Package { get; set; }

        public string? Location { get; set; }

        public double MatchPercentage { get; set; }

        public bool AcademicallyEligible { get; set; }

        public List<string> MatchingSkills { get; set; } =
            new();

        public List<string> MissingSkills { get; set; } =
            new();

        public string Recommendation { get; set; } =
            string.Empty;
    }
}