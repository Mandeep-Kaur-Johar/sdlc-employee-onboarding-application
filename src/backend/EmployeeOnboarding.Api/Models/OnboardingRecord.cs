using System.ComponentModel.DataAnnotations;

namespace EmployeeOnboarding.Api.Models;

/// <summary>
/// Aggregate root created when a candidate accepts an offer (User Story 2788).
/// </summary>
public class OnboardingRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(256)]
    public string CandidateEmail { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Role { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Department { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Location { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }

    [MaxLength(100)]
    public string? ManagerEmail { get; set; }

    [MaxLength(100)]
    public string? OfferReference { get; set; }

    [Required, MaxLength(40)]
    public string Status { get; set; } = OnboardingStatus.Initiated;

    public int CompletionPercentage { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<OnboardingTask> Tasks { get; set; } = new();

    public List<DocumentRecord> Documents { get; set; } = new();

    public List<ProvisioningRequest> ProvisioningRequests { get; set; } = new();

    public List<TrainingAssignment> TrainingAssignments { get; set; } = new();

    public List<Milestone> Milestones { get; set; } = new();

    public string FullName => string.Concat(FirstName, " ", LastName).Trim();
}
