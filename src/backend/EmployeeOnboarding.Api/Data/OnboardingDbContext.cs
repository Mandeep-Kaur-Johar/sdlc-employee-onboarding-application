using EmployeeOnboarding.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EmployeeOnboarding.Api.Data;

/// <summary>
/// EF Core 8 context for the Employee Onboarding Application (Epic 2786).
/// </summary>
public class OnboardingDbContext : DbContext
{
    public OnboardingDbContext(DbContextOptions<OnboardingDbContext> options) : base(options)
    {
    }

    public DbSet<OnboardingRecord> OnboardingRecords => Set<OnboardingRecord>();

    public DbSet<OnboardingTask> OnboardingTasks => Set<OnboardingTask>();

    public DbSet<TaskTemplate> TaskTemplates => Set<TaskTemplate>();

    public DbSet<DocumentRecord> DocumentRecords => Set<DocumentRecord>();

    public DbSet<DocumentReview> DocumentReviews => Set<DocumentReview>();

    public DbSet<ProvisioningRequest> ProvisioningRequests => Set<ProvisioningRequest>();

    public DbSet<TrainingAssignment> TrainingAssignments => Set<TrainingAssignment>();

    public DbSet<Milestone> Milestones => Set<Milestone>();

    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<OnboardingRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.CandidateEmail, e.StartDate }).IsUnique();
            entity.Ignore(e => e.FullName);
            entity.HasMany(e => e.Tasks)
                  .WithOne(t => t.OnboardingRecord!)
                  .HasForeignKey(t => t.OnboardingRecordId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Documents)
                  .WithOne(d => d.OnboardingRecord!)
                  .HasForeignKey(d => d.OnboardingRecordId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.ProvisioningRequests)
                  .WithOne(p => p.OnboardingRecord!)
                  .HasForeignKey(p => p.OnboardingRecordId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.TrainingAssignments)
                  .WithOne(t => t.OnboardingRecord!)
                  .HasForeignKey(t => t.OnboardingRecordId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Milestones)
                  .WithOne(m => m.OnboardingRecord!)
                  .HasForeignKey(m => m.OnboardingRecordId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DocumentRecord>()
                    .HasMany(d => d.Reviews)
                    .WithOne(r => r.DocumentRecord!)
                    .HasForeignKey(r => r.DocumentRecordId)
                    .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OnboardingTask>().HasIndex(t => t.AssignedToEmail);
        modelBuilder.Entity<OnboardingTask>().HasIndex(t => t.Status);
        modelBuilder.Entity<ProvisioningRequest>().HasIndex(p => p.Status);
        modelBuilder.Entity<TaskTemplate>().HasIndex(t => new { t.Role, t.Department, t.Location });
    }
}
