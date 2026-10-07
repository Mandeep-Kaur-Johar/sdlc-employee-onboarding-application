using System.ComponentModel.DataAnnotations;

namespace EmployeeOnboarding.Api.Models;

/// <summary>
/// Immutable audit entry describing an HR approval decision (User Story 2792).
/// </summary>
public class DocumentReview
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid DocumentRecordId { get; set; }

    public DocumentRecord? DocumentRecord { get; set; }

    [Required, MaxLength(256)]
    public string ReviewerEmail { get; set; } = string.Empty;

    /// <summary>Approved or Rejected.</summary>
    [Required, MaxLength(40)]
    public string Decision { get; set; } = DocumentStatus.Approved;

    [MaxLength(1000)]
    public string? Comments { get; set; }

    public DateTime ReviewedAtUtc { get; set; } = DateTime.UtcNow;
}
