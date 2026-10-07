namespace EmployeeOnboarding.Api.Models;

/// <summary>
/// Lifecycle status of an onboarding record (User Story 2788, 2789).
/// </summary>
public static class OnboardingStatus
{
    public const string Initiated = "Initiated";
    public const string InProgress = "InProgress";
    public const string ReadyForDayOne = "ReadyForDayOne";
    public const string Completed = "Completed";

    public static readonly string[] All = { Initiated, InProgress, ReadyForDayOne, Completed };

    public static bool IsValid(string? value) =>
        !string.IsNullOrWhiteSpace(value) && All.Contains(value, StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Status values for generated onboarding tasks (User Story 2794, 2795).
/// </summary>
public static class OnboardingTaskStatus
{
    public const string Pending = "Pending";
    public const string InProgress = "InProgress";
    public const string Completed = "Completed";
    public const string Blocked = "Blocked";

    public static readonly string[] All = { Pending, InProgress, Completed, Blocked };

    public static bool IsValid(string? value) =>
        !string.IsNullOrWhiteSpace(value) && All.Contains(value, StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Status values for collected compliance documents (User Story 2791, 2792).
/// </summary>
public static class DocumentStatus
{
    public const string Submitted = "Submitted";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";

    public static readonly string[] All = { Submitted, Approved, Rejected };
}

/// <summary>
/// Status values for IT provisioning requests (User Story 2797, 2798).
/// </summary>
public static class ProvisioningStatus
{
    public const string Requested = "Requested";
    public const string InProgress = "InProgress";
    public const string Completed = "Completed";
    public const string Failed = "Failed";

    public static readonly string[] All = { Requested, InProgress, Completed, Failed };

    public static bool IsValid(string? value) =>
        !string.IsNullOrWhiteSpace(value) && All.Contains(value, StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Status values for orientation and training assignments (User Story 2801).
/// </summary>
public static class TrainingStatus
{
    public const string NotStarted = "NotStarted";
    public const string InProgress = "InProgress";
    public const string Completed = "Completed";

    public static readonly string[] All = { NotStarted, InProgress, Completed };
}

/// <summary>
/// Application role names used for authorization decisions.
/// </summary>
public static class AppRoles
{
    public const string Employee = "Employee";
    public const string HRCoordinator = "HRCoordinator";
    public const string HRSpecialist = "HRSpecialist";
    public const string ITAdmin = "ITAdmin";
    public const string Manager = "Manager";
}
