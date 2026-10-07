using System.ComponentModel.DataAnnotations;

namespace EmployeeOnboarding.Api.Models;

/// <summary>
/// Request for an IT asset or system account (User Story 2797, 2798).
/// </summary>
public class ProvisioningRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OnboardingRecordId { get; set; }

    public OnboardingRecord? OnboardingRecord { get; set; }

    /// <summary>Equipment or Account.</summary>
    [Required, MaxLength(60)]
    public string ItemType { get; set; } = "Account";

    [Required, MaxLength(150)]
    public string SystemName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Details { get; set; }

    [MaxLength(100)]
    public string? ExternalTicketId { get; set; }

    [Required, MaxLength(40)]
    public string Status { get; set; } = ProvisioningStatus.Requested;

    public DateOnly RequiredByDate { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAtUtc { get; set; }

    [MaxLength(256)]
    public string? UpdatedBySystem { get; set; }
}
