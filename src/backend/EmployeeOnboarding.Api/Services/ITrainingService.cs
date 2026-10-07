using EmployeeOnboarding.Api.DTOs;

namespace EmployeeOnboarding.Api.Services;

/// <summary>
/// Orientation and training tracking (User Story 2801).
/// </summary>
public interface ITrainingService
{
    Task<IReadOnlyList<TrainingAssignmentDto>?> GetForRecordAsync(Guid onboardingRecordId, CancellationToken cancellationToken = default);

    Task<TrainingAssignmentDto?> StartAsync(Guid assignmentId, CancellationToken cancellationToken = default);

    Task<TrainingAssignmentDto?> CompleteAsync(Guid assignmentId, CompleteTrainingDto request, CancellationToken cancellationToken = default);
}
