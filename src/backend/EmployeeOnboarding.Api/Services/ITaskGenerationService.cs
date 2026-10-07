using EmployeeOnboarding.Api.DTOs;
using EmployeeOnboarding.Api.Models;

namespace EmployeeOnboarding.Api.Services;

/// <summary>
/// Generates onboarding tasks from templates and manages task status
/// (User Story 2794).
/// </summary>
public interface ITaskGenerationService
{
    Task<IReadOnlyList<OnboardingTaskDto>> GenerateForRecordAsync(Guid onboardingRecordId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OnboardingTaskDto>> GetTasksAsync(Guid? onboardingRecordId, string? assignedToEmail, string? status, bool overdueOnly, CancellationToken cancellationToken = default);

    Task<OnboardingTaskDto?> UpdateStatusAsync(Guid taskId, string status, CancellationToken cancellationToken = default);

    IReadOnlyList<TaskTemplate> SelectTemplates(IEnumerable<TaskTemplate> templates, OnboardingRecord record);
}
