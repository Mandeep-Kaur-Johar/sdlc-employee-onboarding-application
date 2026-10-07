using System.ComponentModel.DataAnnotations;

namespace EmployeeOnboarding.Api.Models;

/// <summary>
/// Progress milestone displayed on the HR dashboard (User Story 2789).
/// </summary>
public class Milestone
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OnboardingRecordId { get; set; }

    public OnboardingRecord? OnboardingRecord { get; set; }

    [Required, MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(40)]
    public string Status { get; set; } = "Pending";

    public DateOnly TargetDate { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public int SortOrder { get; set; }
}
