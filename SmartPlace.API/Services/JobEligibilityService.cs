using SmartPlace.API.Models;

namespace SmartPlace.API.Services;

public class JobEligibilityService
{
    public JobEligibilityResult Evaluate(
        Student student,
        Job job)
    {
        var tenthEligible =
            student.TenthPercentage >=
            job.MinimumTenthPercentage;

        var twelfthEligible =
            student.TwelfthPercentage >=
            job.MinimumTwelfthPercentage;

        var cgpaEligible =
            student.CGPA >=
            job.MinimumCGPA;

        var backlogEligible =
            student.Backlogs <=
            job.MaximumBacklogs;

        var graduationYearEligible =
            student.GraduationYear ==
            job.GraduationYear;

        // A job without a configured department
        // requirement is treated as NOT eligible.
        //
        // This affects old jobs created before the
        // new eligibility system was introduced.
        var departmentEligible =
            job.RequiredDepartmentId.HasValue
            &&
            student.DepartmentId ==
            job.RequiredDepartmentId.Value;

        var reasons =
            new List<string>();

        if (!job.RequiredDepartmentId.HasValue)
        {
            reasons.Add(
                "The required department has not yet been configured for this job.");
        }
        else if (!departmentEligible)
        {
            reasons.Add(
                $"Required department: " +
                $"{job.RequiredDepartment?.Name ?? "specified department"}.");
        }

        if (!tenthEligible)
        {
            reasons.Add(
                $"Minimum 10th percentage: " +
                $"{job.MinimumTenthPercentage}%.");
        }

        if (!twelfthEligible)
        {
            reasons.Add(
                $"Minimum 12th percentage: " +
                $"{job.MinimumTwelfthPercentage}%.");
        }

        if (!cgpaEligible)
        {
            reasons.Add(
                $"Minimum CGPA: " +
                $"{job.MinimumCGPA}.");
        }

        if (!backlogEligible)
        {
            reasons.Add(
                $"Maximum allowed backlogs: " +
                $"{job.MaximumBacklogs}.");
        }

        if (!graduationYearEligible)
        {
            reasons.Add(
                $"Required graduation year: " +
                $"{job.GraduationYear}.");
        }

        return new JobEligibilityResult
        {
            IsEligible =
                departmentEligible
                &&
                tenthEligible
                &&
                twelfthEligible
                &&
                cgpaEligible
                &&
                backlogEligible
                &&
                graduationYearEligible,

            DepartmentEligible =
                departmentEligible,

            TenthEligible =
                tenthEligible,

            TwelfthEligible =
                twelfthEligible,

            CGPAEligible =
                cgpaEligible,

            BacklogEligible =
                backlogEligible,

            GraduationYearEligible =
                graduationYearEligible,

            Reasons =
                reasons
        };
    }
}

public class JobEligibilityResult
{
    public bool IsEligible
    { get; set; }

    public bool DepartmentEligible
    { get; set; }

    public bool TenthEligible
    { get; set; }

    public bool TwelfthEligible
    { get; set; }

    public bool CGPAEligible
    { get; set; }

    public bool BacklogEligible
    { get; set; }

    public bool GraduationYearEligible
    { get; set; }

    public List<string> Reasons
    { get; set; } =
        new();
}