using System.ComponentModel.DataAnnotations;

namespace EmployeeOnboarding.Api.Models;

/// <summary>
/// Task generated from a template for an onboarding record (User Story 2794, 2795).
/// </summary>
public class OnboardingTask
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OnboardingRecordId { get; set; }

    public OnboardingRecord? OnboardingRecord { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required, MaxLength(100)]
    public string Category { get; set; } = "General";

    [Required, MaxLength(256)]
    public string AssignedToEmail { get; set; } = string.Empty;

    [Required, MaxLength(60)]
    public string AssigneeRole { get; set; } = AppRoles.Employee;

    public DateOnly DueDate { get; set; }

    [Required, MaxLength(40)]
    public string Status { get; set; } = OnboardingTaskStatus.Pending;

    public bool IsEscalated { get; set; }

    public DateTime? LastReminderSentUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAtUtc { get; set; }

    public Guid? TaskTemplateId { get; set; }

    public bool IsOverdue(DateOnly today) =>
        Status != OnboardingTaskStatus.Completed && DueDate < today;
}
