using Microsoft.EntityFrameworkCore;
using SmartPlace.API.Models;

namespace SmartPlace.API.Data;

public class SmartPlaceDbContext : DbContext
{
    public SmartPlaceDbContext(DbContextOptions<SmartPlaceDbContext> options)
        : base(options)
    {
    }

    public DbSet<Student> Students { get; set; }
    public DbSet<Department> Departments { get; set; }
    public DbSet<Skill> Skills { get; set; }
    public DbSet<StudentSkill> StudentSkills { get; set; }

    public DbSet<Company> Companies { get; set; }
    public DbSet<Job> Jobs { get; set; }
    public DbSet<Application> Applications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // -----------------------------
        // STUDENT
        // -----------------------------

        modelBuilder.Entity<Student>()
            .Property(s => s.CGPA)
            .HasPrecision(3, 2);

        modelBuilder.Entity<Student>()
            .HasIndex(s => s.Email)
            .IsUnique();

        // Department -> Students
        modelBuilder.Entity<Student>()
            .HasOne(s => s.Department)
            .WithMany(d => d.Students)
            .HasForeignKey(s => s.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        // -----------------------------
        // DEPARTMENT
        // -----------------------------

        modelBuilder.Entity<Department>()
            .HasIndex(d => d.Name)
            .IsUnique();

        // -----------------------------
        // SKILLS
        // -----------------------------

        modelBuilder.Entity<Skill>()
            .HasIndex(s => s.Name)
            .IsUnique();

        // Composite primary key
        modelBuilder.Entity<StudentSkill>()
            .HasKey(ss => new
            {
                ss.StudentId,
                ss.SkillId
            });

        // Student -> StudentSkill
        modelBuilder.Entity<StudentSkill>()
            .HasOne(ss => ss.Student)
            .WithMany(s => s.StudentSkills)
            .HasForeignKey(ss => ss.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Skill -> StudentSkill
        modelBuilder.Entity<StudentSkill>()
            .HasOne(ss => ss.Skill)
            .WithMany(s => s.StudentSkills)
            .HasForeignKey(ss => ss.SkillId)
            .OnDelete(DeleteBehavior.Cascade);

        // -----------------------------
        // COMPANY
        // -----------------------------

        modelBuilder.Entity<Company>()
            .HasIndex(c => c.Name)
            .IsUnique();

        // Company -> Jobs
        modelBuilder.Entity<Job>()
            .HasOne(j => j.Company)
            .WithMany(c => c.Jobs)
            .HasForeignKey(j => j.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        // -----------------------------
        // JOB
        // -----------------------------

        modelBuilder.Entity<Job>()
            .Property(j => j.Package)
            .HasPrecision(10, 2);

        modelBuilder.Entity<Job>()
            .Property(j => j.MinimumCGPA)
            .HasPrecision(3, 2);

        // -----------------------------
        // APPLICATION
        // -----------------------------

        // Student -> Applications
        modelBuilder.Entity<Application>()
            .HasOne(a => a.Student)
            .WithMany(s => s.Applications)
            .HasForeignKey(a => a.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Job -> Applications
        modelBuilder.Entity<Application>()
            .HasOne(a => a.Job)
            .WithMany(j => j.Applications)
            .HasForeignKey(a => a.JobId)
            .OnDelete(DeleteBehavior.Restrict);

        // Prevent duplicate application
        // Same student cannot apply twice for same job
        modelBuilder.Entity<Application>()
            .HasIndex(a => new
            {
                a.StudentId,
                a.JobId
            })
            .IsUnique();
    }
}