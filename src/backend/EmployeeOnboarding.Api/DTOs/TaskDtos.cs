using System.ComponentModel.DataAnnotations;

namespace EmployeeOnboarding.Api.DTOs;

public class OnboardingTaskDto
{
    public Guid Id { get; set; }
    public Guid OnboardingRecordId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public string AssignedToEmail { get; set; } = string.Empty;
    public string AssigneeRole { get; set; } = string.Empty;
    public DateOnly DueDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsOverdue { get; set; }
    public bool IsEscalated { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}

public class UpdateTaskStatusDto
{
    [Required, MaxLength(40)]
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Result of running the reminder and escalation sweep (User Story 2795).
/// </summary>
public class ReminderRunResultDto
{
    public int RemindersSent { get; set; }
    public int EscalationsSent { get; set; }
    public DateTime ExecutedAtUtc { get; set; } = DateTime.UtcNow;
    public List<NotificationDto> Notifications { get; set; } = new();
}

public class NotificationDto
{
    public Guid Id { get; set; }
    public string Recipient { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime SentAtUtc { get; set; }
}
