using System.ComponentModel.DataAnnotations;

namespace EmployeeOnboarding.Api.DTOs;

/// <summary>
/// Accepted-offer event payload published by the HR system (User Story 2788).
/// </summary>
public class AcceptedOfferEventDto
{
    [Required, EmailAddress, MaxLength(256)]
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

    [Required]
    public DateOnly StartDate { get; set; }

    [EmailAddress, MaxLength(100)]
    public string? ManagerEmail { get; set; }

    [MaxLength(100)]
    public string? OfferReference { get; set; }
}

public class OnboardingRecordDto
{
    public Guid Id { get; set; }
    public string CandidateEmail { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public string? ManagerEmail { get; set; }
    public string? OfferReference { get; set; }
    public string Status { get; set; } = string.Empty;
    public int CompletionPercentage { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public class MilestoneDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateOnly TargetDate { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>
/// Dashboard payload showing status, milestones, overdue tasks and completion
/// percentage (User Story 2789).
/// </summary>
public class OnboardingProgressDto
{
    public Guid OnboardingRecordId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public int CompletionPercentage { get; set; }
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int OverdueTasks { get; set; }
    public int PendingDocuments { get; set; }
    public int ApprovedDocuments { get; set; }
    public int RejectedDocuments { get; set; }
    public int PendingProvisioning { get; set; }
    public int CompletedProvisioning { get; set; }
    public int CompletedTraining { get; set; }
    public int TotalTraining { get; set; }
    public List<MilestoneDto> Milestones { get; set; } = new();
}

public class OnboardingDetailDto
{
    public OnboardingRecordDto Record { get; set; } = new();
    public OnboardingProgressDto Progress { get; set; } = new();
    public List<OnboardingTaskDto> Tasks { get; set; } = new();
    public List<DocumentRecordDto> Documents { get; set; } = new();
    public List<ProvisioningRequestDto> ProvisioningRequests { get; set; } = new();
    public List<TrainingAssignmentDto> TrainingAssignments { get; set; } = new();
}

public class UpdateOnboardingStatusDto
{
    [Required, MaxLength(40)]
    public string Status { get; set; } = string.Empty;
}
