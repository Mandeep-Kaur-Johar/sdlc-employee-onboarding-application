using System.ComponentModel.DataAnnotations;

namespace EmployeeOnboarding.Api.Models;

/// <summary>
/// Orientation or training course assigned to a new employee (User Story 2801).
/// </summary>
public class TrainingAssignment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OnboardingRecordId { get; set; }

    public OnboardingRecord? OnboardingRecord { get; set; }

    [Required, MaxLength(60)]
    public string CourseCode { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string CourseName { get; set; } = string.Empty;

    public bool IsMandatory { get; set; } = true;

    [MaxLength(1024)]
    public string? ContentUrl { get; set; }

    [Required, MaxLength(40)]
    public string Status { get; set; } = TrainingStatus.NotStarted;

    public int? ScorePercentage { get; set; }

    public DateOnly DueDate { get; set; }

    public DateTime? CompletedAtUtc { get; set; }
}
