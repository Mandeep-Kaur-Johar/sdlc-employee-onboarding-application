using System.ComponentModel.DataAnnotations;

namespace EmployeeOnboarding.Api.Models;

/// <summary>
/// Audit record of a notification, reminder or escalation (User Story 2792, 2795).
/// </summary>
public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? OnboardingRecordId { get; set; }

    public Guid? OnboardingTaskId { get; set; }

    public Guid? DocumentRecordId { get; set; }

    [Required, MaxLength(256)]
    public string Recipient { get; set; } = string.Empty;

    [Required, MaxLength(40)]
    public string Channel { get; set; } = "Email";

    /// <summary>Welcome, TaskAssigned, Reminder, Escalation, DocumentApproved, DocumentRejected.</summary>
    [Required, MaxLength(60)]
    public string Kind { get; set; } = "Reminder";

    [Required, MaxLength(300)]
    public string Subject { get; set; } = string.Empty;

    [Required, MaxLength(2000)]
    public string Body { get; set; } = string.Empty;

    public DateTime SentAtUtc { get; set; } = DateTime.UtcNow;
}
