namespace SmartPlace.API.Models;

public class Job
{
    public int JobId { get; set; }

    public string Title { get; set; } =
        string.Empty;

    public string Description { get; set; } =
        string.Empty;

    public decimal Package { get; set; }

    // --------------------------------------------------
    // ACADEMIC ELIGIBILITY REQUIREMENTS
    // --------------------------------------------------

    public decimal MinimumTenthPercentage
    { get; set; }

    public decimal MinimumTwelfthPercentage
    { get; set; }

    public decimal MinimumCGPA
    { get; set; }

    public int MaximumBacklogs
    { get; set; }

    public int GraduationYear
    { get; set; }

    // --------------------------------------------------
    // REQUIRED DEPARTMENT / BRANCH
    //
    // Nullable only so old jobs already present in the
    // database can survive the migration.
    //
    // New jobs are still required to provide this value
    // through validation in JobsController.
    // --------------------------------------------------

    public int? RequiredDepartmentId
    { get; set; }

    public Department? RequiredDepartment
    { get; set; }

    // --------------------------------------------------
    // JOB DETAILS
    // --------------------------------------------------

    public string Location { get; set; } =
        string.Empty;

    public string EmploymentType { get; set; } =
        "Full-Time";

    public DateTime PostedDate { get; set; } =
        DateTime.UtcNow;

    public DateTime? ApplicationDeadline
    { get; set; }

    public string Status { get; set; } =
        "Pending";

    // --------------------------------------------------
    // COMPANY
    // --------------------------------------------------

    public int CompanyId { get; set; }

    public Company? Company { get; set; }

    // --------------------------------------------------
    // APPLICATIONS
    // --------------------------------------------------

    public ICollection<Application> Applications
    { get; set; } =
        new List<Application>();
}