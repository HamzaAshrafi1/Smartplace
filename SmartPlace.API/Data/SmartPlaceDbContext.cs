using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartPlace.API.Models;

namespace SmartPlace.API.Data;

public class SmartPlaceDbContext : IdentityDbContext<ApplicationUser>
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

    public DbSet<InterviewRound> InterviewRounds { get; set; }

    public DbSet<Placement> Placements { get; set; }

    public DbSet<Resume> Resumes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Student

        modelBuilder.Entity<Student>()
            .Property(s => s.CGPA)
            .HasPrecision(3, 2);

        modelBuilder.Entity<Student>()
            .HasIndex(s => s.Email)
            .IsUnique();

        modelBuilder.Entity<Student>()
            .HasOne(s => s.Department)
            .WithMany(d => d.Students)
            .HasForeignKey(s => s.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Department

        modelBuilder.Entity<Department>()
            .HasIndex(d => d.Name)
            .IsUnique();

        // Skill

        modelBuilder.Entity<Skill>()
            .HasIndex(s => s.Name)
            .IsUnique();

        // StudentSkill

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

        // Company

        modelBuilder.Entity<Company>()
            .HasIndex(c => c.Name)
            .IsUnique();

        // Job

        modelBuilder.Entity<Job>()
            .HasOne(j => j.Company)
            .WithMany(c => c.Jobs)
            .HasForeignKey(j => j.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Job>()
            .Property(j => j.Package)
            .HasPrecision(10, 2);

        modelBuilder.Entity<Job>()
            .Property(j => j.MinimumCGPA)
            .HasPrecision(3, 2);

        // Application

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

        // InterviewRound

        modelBuilder.Entity<InterviewRound>()
            .HasOne(i => i.Application)
            .WithMany(a => a.InterviewRounds)
            .HasForeignKey(i => i.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Placement

        modelBuilder.Entity<Placement>()
            .Property(p => p.OfferedPackage)
            .HasPrecision(10, 2);

        modelBuilder.Entity<Placement>()
            .HasOne(p => p.Student)
            .WithOne(s => s.Placement)
            .HasForeignKey<Placement>(p => p.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Placement>()
            .HasOne(p => p.Company)
            .WithMany(c => c.Placements)
            .HasForeignKey(p => p.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Placement>()
            .HasIndex(p => p.StudentId)
            .IsUnique();

        // Resume

        modelBuilder.Entity<Resume>()
            .HasOne(r => r.Student)
            .WithOne(s => s.Resume)
            .HasForeignKey<Resume>(r => r.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Resume>()
            .HasIndex(r => r.StudentId)
            .IsUnique();
    }
}