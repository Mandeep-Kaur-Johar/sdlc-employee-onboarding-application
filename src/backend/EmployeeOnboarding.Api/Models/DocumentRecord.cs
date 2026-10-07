using System.ComponentModel.DataAnnotations;

namespace EmployeeOnboarding.Api.Models;

/// <summary>
/// Metadata for a securely stored onboarding document (User Story 2791, 2792).
/// Binary content is stored outside the database.
/// </summary>
public class DocumentRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OnboardingRecordId { get; set; }

    public OnboardingRecord? OnboardingRecord { get; set; }

    [Required, MaxLength(100)]
    public string DocumentType { get; set; } = string.Empty;

    [Required, MaxLength(260)]
    public string FileName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    [Required, MaxLength(1024)]
    public string StoragePath { get; set; } = string.Empty;

    [Required, MaxLength(40)]
    public string Status { get; set; } = DocumentStatus.Submitted;

    [Required, MaxLength(256)]
    public string UploadedByEmail { get; set; } = string.Empty;

    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;

    public List<DocumentReview> Reviews { get; set; } = new();
}
