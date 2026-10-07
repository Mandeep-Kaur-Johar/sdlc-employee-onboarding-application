using System.ComponentModel.DataAnnotations;

namespace EmployeeOnboarding.Api.DTOs;

public class ProvisioningRequestDto
{
    public Guid Id { get; set; }
    public Guid OnboardingRecordId { get; set; }
    public string ItemType { get; set; } = string.Empty;
    public string SystemName { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string? ExternalTicketId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateOnly RequiredByDate { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? UpdatedBySystem { get; set; }
}

public class CreateProvisioningRequestDto
{
    [Required, MaxLength(60)]
    public string ItemType { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string SystemName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Details { get; set; }
}

/// <summary>
/// Status callback from an integrated provisioning system (User Story 2798).
/// </summary>
public class UpdateProvisioningStatusDto
{
    [Required, MaxLength(40)]
    public string Status { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? ExternalTicketId { get; set; }

    [MaxLength(256)]
    public string? UpdatedBySystem { get; set; }
}

/// <summary>
/// Aggregated provisioning readiness view (User Story 2798).
/// </summary>
public class ProvisioningSummaryDto
{
    public Guid OnboardingRecordId { get; set; }
    public int TotalRequests { get; set; }
    public int CompletedRequests { get; set; }
    public int FailedRequests { get; set; }
    public int CompletionPercentage { get; set; }
    public bool IsDayOneReady { get; set; }
    public List<ProvisioningRequestDto> Requests { get; set; } = new();
}
