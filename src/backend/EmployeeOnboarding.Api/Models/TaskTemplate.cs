using System.ComponentModel.DataAnnotations;

namespace EmployeeOnboarding.Api.Models;

/// <summary>
/// Rule-bearing template used to generate onboarding tasks based on role,
/// department and location (User Story 2794).
/// </summary>
public class TaskTemplate
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required, MaxLength(100)]
    public string Category { get; set; } = "General";

    /// <summary>Role filter. Null or "*" applies to every role.</summary>
    [MaxLength(100)]
    public string? Role { get; set; }

    /// <summary>Department filter. Null or "*" applies to every department.</summary>
    [MaxLength(100)]
    public string? Department { get; set; }

    /// <summary>Location filter. Null or "*" applies to every location.</summary>
    [MaxLength(100)]
    public string? Location { get; set; }

    /// <summary>Days relative to the start date when the task is due.</summary>
    public int OffsetDays { get; set; }

    [Required, MaxLength(60)]
    public string AssigneeRole { get; set; } = AppRoles.Employee;

    public bool IsActive { get; set; } = true;
}
