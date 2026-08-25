using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartPlace.API.Models;

namespace SmartPlace.API.Data;

public class SmartPlaceDbContext
    : IdentityDbContext<ApplicationUser>
{
    public SmartPlaceDbContext(
        DbContextOptions<SmartPlaceDbContext> options)
        : base(options)
    {
    }

    public DbSet<Student> Students => Set<Student>();

    public DbSet<Department> Departments =>
        Set<Department>();

    public DbSet<Skill> Skills => Set<Skill>();

    public DbSet<StudentSkill> StudentSkills =>
        Set<StudentSkill>();

    public DbSet<Company> Companies =>
        Set<Company>();

    public DbSet<Job> Jobs => Set<Job>();

    public DbSet<Application> Applications =>
        Set<Application>();

    public DbSet<InterviewRound> InterviewRounds =>
        Set<InterviewRound>();

    public DbSet<Placement> Placements =>
        Set<Placement>();

    public DbSet<Resume> Resumes =>
        Set<Resume>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ==================================================
        // STUDENT
        // ==================================================

        modelBuilder.Entity<Student>()
            .Property(s => s.CGPA)
            .HasPrecision(4, 2);

        modelBuilder.Entity<Student>()
            .Property(s => s.TenthPercentage)
            .HasPrecision(5, 2);

        modelBuilder.Entity<Student>()
            .Property(s => s.TwelfthPercentage)
            .HasPrecision(5, 2);

        modelBuilder.Entity<Student>()
            .HasIndex(s => s.Email)
            .IsUnique();

        // Student <-> Identity User
        modelBuilder.Entity<Student>()
            .HasOne(s => s.ApplicationUser)
            .WithOne(u => u.Student)
            .HasForeignKey<Student>(
                s => s.ApplicationUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Student>()
            .HasIndex(s => s.ApplicationUserId)
            .IsUnique()
            .HasFilter(
                "[ApplicationUserId] IS NOT NULL");

        // Student -> Department
        modelBuilder.Entity<Student>()
            .HasOne(s => s.Department)
            .WithMany(d => d.Students)
            .HasForeignKey(s => s.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        // ==================================================
        // STUDENT SKILLS
        // ==================================================

        modelBuilder.Entity<StudentSkill>()
            .HasKey(ss => new
            {
                ss.StudentId,
                ss.SkillId
            });

        modelBuilder.Entity<StudentSkill>()
            .HasOne(ss => ss.Student)
            .WithMany(s => s.StudentSkills)
            .HasForeignKey(ss => ss.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<StudentSkill>()
            .HasOne(ss => ss.Skill)
            .WithMany(s => s.StudentSkills)
            .HasForeignKey(ss => ss.SkillId)
            .OnDelete(DeleteBehavior.Cascade);

        // ==================================================
        // COMPANY
        // ==================================================

        modelBuilder.Entity<Company>()
            .HasOne(c => c.RecruiterUser)
            .WithOne(u => u.Company)
            .HasForeignKey<Company>(
                c => c.RecruiterUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Company>()
            .HasIndex(c => c.RecruiterUserId)
            .IsUnique()
            .HasFilter(
                "[RecruiterUserId] IS NOT NULL");

        // ==================================================
        // JOB
        // ==================================================

        modelBuilder.Entity<Job>()
            .Property(j => j.Package)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Job>()
            .Property(j => j.MinimumCGPA)
            .HasPrecision(4, 2);

        modelBuilder.Entity<Job>()
            .Property(
                j => j.MinimumTenthPercentage)
            .HasPrecision(5, 2);

        modelBuilder.Entity<Job>()
            .Property(
                j => j.MinimumTwelfthPercentage)
            .HasPrecision(5, 2);

        modelBuilder.Entity<Job>()
            .HasOne(j => j.Company)
            .WithMany(c => c.Jobs)
            .HasForeignKey(j => j.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Job>()
            .HasOne(j => j.RequiredDepartment)
            .WithMany()
            .HasForeignKey(
                j => j.RequiredDepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        // ==================================================
        // APPLICATION
        // ==================================================

        modelBuilder.Entity<Application>()
            .HasOne(a => a.Student)
            .WithMany(s => s.Applications)
            .HasForeignKey(a => a.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Application>()
            .HasOne(a => a.Job)
            .WithMany(j => j.Applications)
            .HasForeignKey(a => a.JobId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Application>()
            .HasIndex(a => new
            {
                a.StudentId,
                a.JobId
            })
            .IsUnique();

        // ==================================================
        // INTERVIEW
        // ==================================================

        modelBuilder.Entity<InterviewRound>()
            .HasOne(i => i.Application)
            .WithMany(a => a.InterviewRounds)
            .HasForeignKey(i => i.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        // ==================================================
        // PLACEMENT
        // ==================================================

        modelBuilder.Entity<Placement>()
            .Property(p => p.OfferedPackage)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Placement>()
            .HasOne(p => p.Student)
            .WithOne(s => s.Placement)
            .HasForeignKey<Placement>(
                p => p.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Placement>()
            .HasOne(p => p.Company)
            .WithMany(c => c.Placements)
            .HasForeignKey(p => p.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Placement>()
            .HasIndex(p => p.StudentId)
            .IsUnique();

        // ==================================================
        // RESUME
        // ==================================================

        modelBuilder.Entity<Resume>()
            .HasOne(r => r.Student)
            .WithOne(s => s.Resume)
            .HasForeignKey<Resume>(
                r => r.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Resume>()
            .HasIndex(r => r.StudentId)
            .IsUnique();

        // ==================================================
        // UNIQUE MASTER DATA
        // ==================================================

        modelBuilder.Entity<Department>()
            .HasIndex(d => d.Name)
            .IsUnique();

        modelBuilder.Entity<Skill>()
            .HasIndex(s => s.Name)
            .IsUnique();
    }
}