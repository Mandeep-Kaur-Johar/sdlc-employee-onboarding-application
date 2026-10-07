using System.ComponentModel.DataAnnotations;

namespace EmployeeOnboarding.Api.DTOs;

public class DocumentRecordDto
{
    public Guid Id { get; set; }
    public Guid OnboardingRecordId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string Status { get; set; } = string.Empty;
    public string UploadedByEmail { get; set; } = string.Empty;
    public DateTime UploadedAtUtc { get; set; }
    public string? LatestReviewComments { get; set; }
    public string? LatestReviewerEmail { get; set; }
    public DateTime? LatestReviewedAtUtc { get; set; }
}

/// <summary>
/// Review decision submitted by HR (User Story 2792).
/// </summary>
public class DocumentReviewRequestDto
{
    [Required, MaxLength(40)]
    public string Decision { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Comments { get; set; }

    [Required, EmailAddress, MaxLength(256)]
    public string ReviewerEmail { get; set; } = string.Empty;
}

/// <summary>
/// Completion tracking for the required document checklist (User Story 2791).
/// </summary>
public class DocumentComplianceDto
{
    public Guid OnboardingRecordId { get; set; }
    public int RequiredCount { get; set; }
    public int SubmittedCount { get; set; }
    public int ApprovedCount { get; set; }
    public int RejectedCount { get; set; }
    public int CompletionPercentage { get; set; }
    public List<string> MissingDocumentTypes { get; set; } = new();
    public List<DocumentRecordDto> Documents { get; set; } = new();
}
