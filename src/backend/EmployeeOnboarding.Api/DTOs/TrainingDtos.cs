using System.ComponentModel.DataAnnotations;

namespace EmployeeOnboarding.Api.DTOs;

public class TrainingAssignmentDto
{
    public Guid Id { get; set; }
    public Guid OnboardingRecordId { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public bool IsMandatory { get; set; }
    public string? ContentUrl { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? ScorePercentage { get; set; }
    public DateOnly DueDate { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}

/// <summary>
/// Training completion record visible to HR and managers (User Story 2801).
/// </summary>
public class CompleteTrainingDto
{
    [Range(0, 100)]
    public int? ScorePercentage { get; set; }
}

/// <summary>
/// Personalized portal payload for the signed-in employee (User Story 2800).
/// </summary>
public class PortalDto
{
    public OnboardingRecordDto Record { get; set; } = new();
    public OnboardingProgressDto Progress { get; set; } = new();
    public List<OnboardingTaskDto> MyTasks { get; set; } = new();
    public List<DocumentRecordDto> MyDocuments { get; set; } = new();
    public List<TrainingAssignmentDto> MyTraining { get; set; } = new();
    public List<string> RequiredDocumentTypes { get; set; } = new();
}
